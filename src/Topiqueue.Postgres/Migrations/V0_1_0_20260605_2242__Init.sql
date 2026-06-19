CREATE TABLE IF NOT EXISTS ${topic_table}
(
    topic_name TEXT NOT NULL PRIMARY KEY,
    topic_seq_id INT NOT NULL GENERATED ALWAYS AS IDENTITY,
    created_at timestamptz NOT NULL DEFAULT now(),
    partitions_count INT NOT NULL,
    retention_interval INTERVAL NOT NULL
);

CREATE TABLE IF NOT EXISTS ${topic_segment_table}
(
    id INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    topic_name TEXT NOT NULL,
    segment_start TIMESTAMPTZ NOT NULL,
    segment_end TIMESTAMPTZ NOT NULL
);

CREATE INDEX IF NOT EXISTS "${prefix}topic_topic_name_segment_end_idx" 
    ON ${topic_segment_table}(topic_name, segment_end);

CREATE SEQUENCE IF NOT EXISTS "${prefix}message_seq" AS BIGINT;

CREATE TABLE IF NOT EXISTS ${message_table}
(
    topic_name text NOT NULL,
    partition_num INT NOT NULL,
    tx_id xid8 NOT NULL DEFAULT pg_current_xact_id(),
    seq_id bigint NOT NULL DEFAULT nextval('"${prefix}message_seq"'),
    created_at timestamptz NOT NULL DEFAULT now(),
    partition_key text DEFAULT NULL,
    message_type text NOT NULL,
    data_txt text DEFAULT NULL,
    data_bin bytea DEFAULT NULL
) PARTITION BY LIST(topic_name);

CREATE INDEX IF NOT EXISTS "${prefix}message_key_idx" 
    ON ${message_table}(partition_num, tx_id, seq_id);


CREATE OR REPLACE FUNCTION ${ensure_topic_created_function}(
    p_topic_name TEXT,
    p_partitions_count INT,
    p_retention_interval INTERVAL
)
RETURNS TABLE (
    topic_name TEXT,
    topic_seq_id INT,
    partitions_count INT,
    retention_interval INTERVAL,
    created_now BOOLEAN
)
LANGUAGE plpgsql
AS $$
DECLARE
    schema_name TEXT;
    topic_table_name TEXT;
    query TEXT;
BEGIN
    SELECT t.topic_name, t.topic_seq_id, 
           t.partitions_count, t.retention_interval, FALSE
    INTO topic_name, topic_seq_id, partitions_count, retention_interval, created_now
    FROM ${topic_table} t
    WHERE t.topic_name = p_topic_name;
    
    IF FOUND THEN
        RETURN NEXT;
        RETURN;
    END IF;

    PERFORM pg_advisory_lock(hashtext(p_topic_name));

    SELECT t.topic_name, t.topic_seq_id,
           t.partitions_count, t.retention_interval, FALSE
    INTO topic_name, topic_seq_id, partitions_count, retention_interval, created_now
    FROM ${topic_table} t
    WHERE t.topic_name = p_topic_name;
    
    IF FOUND THEN
        RETURN NEXT;
        RETURN;
    END IF;
    
    INSERT INTO ${topic_table} as t (topic_name, partitions_count, retention_interval)
    VALUES (p_topic_name, p_partitions_count, p_retention_interval)
    RETURNING t.topic_name, t.topic_seq_id,
              t.partitions_count, t.retention_interval, TRUE
    INTO topic_name, topic_seq_id, partitions_count, retention_interval, created_now;

    schema_name := '${schema}';
    topic_table_name := '${prefix}message' || '_' || topic_seq_id::text;
    IF schema_name = '' THEN    
        query := format('CREATE TABLE IF NOT EXISTS %I PARTITION OF ${message_table} FOR VALUES IN (%L) PARTITION BY RANGE (created_at)', 
            topic_table_name, topic_name);
    ELSE
        query := format('CREATE TABLE IF NOT EXISTS %I.%I PARTITION OF ${message_table} FOR VALUES IN (%L) PARTITION BY RANGE (created_at)',
            schema_name, topic_table_name, topic_name);
    END IF;
    
    EXECUTE query;
    
    RETURN NEXT;
END;
$$;


CREATE OR REPLACE FUNCTION ${create_topic_segment_function}(
    p_topic_name TEXT,
    p_topic_id INT,
    p_segment_start TIMESTAMPTZ,
    p_segment_end TIMESTAMPTZ
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    schema_name TEXT;
    topic_table_name TEXT;
    segment_table_name TEXT;
    query TEXT;
BEGIN
    INSERT INTO ${topic_segment_table} (topic_name, segment_start, segment_end)
    VALUES (p_topic_name, p_segment_start, p_segment_end);

    schema_name := '${schema}';
    topic_table_name := '${prefix}message' || '_' || p_topic_id::text;
    segment_table_name := topic_table_name || '_' || to_char(p_segment_start, 'YYYYMMDDHH24MI') || '_' || to_char(p_segment_end, 'YYYYMMDDHH24MI');   
    IF schema_name = '' THEN    
        query := format('CREATE TABLE IF NOT EXISTS %I PARTITION OF %I FOR VALUES FROM (%L) TO (%L)', 
            segment_table_name, topic_table_name, p_segment_start, p_segment_end);
    ELSE
        query := format('CREATE TABLE IF NOT EXISTS %I.%I PARTITION OF %I.%I FOR VALUES FROM (%L) TO (%L)', 
            schema_name, segment_table_name, schema_name, topic_table_name, p_segment_start, p_segment_end);
    END IF;
    
    EXECUTE query;
END;
$$;


CREATE OR REPLACE FUNCTION ${ensure_topic_has_segment_function}(
    p_topic_name TEXT,
    threshold INTERVAL
)
RETURNS TABLE (
    created_segment_start TIMESTAMPTZ,
    created_segment_end TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
DECLARE
    topic RECORD;
    latest_end TIMESTAMPTZ;
    new_start TIMESTAMPTZ;
    new_end TIMESTAMPTZ;
BEGIN
    PERFORM pg_advisory_lock(hashtext(p_topic_name));

    SELECT segment_end FROM ${topic_segment_table} 
    WHERE topic_name = p_topic_name
    ORDER BY segment_end DESC
    LIMIT 1
    INTO latest_end;

    IF (latest_end IS NOT NULL) AND (now() < latest_end - threshold) THEN
        created_segment_start = NULL;
        created_segment_end = NULL;
        RETURN NEXT;
        RETURN;
    END IF;
    
    SELECT topic_name, topic_seq_id, retention_interval INTO topic 
    FROM ${topic_table} WHERE topic_name = p_topic_name;

    IF NOT FOUND THEN
        RETURN;
    END IF;

    IF latest_end IS NULL THEN
        new_start := now() - threshold;
    ELSE
        new_start := GREATEST(now() - threshold, latest_end);
    END IF;

    new_start := date_trunc('minute', new_start);
    new_end := GREATEST(new_start + topic.retention_interval, now() + threshold);
    new_end := date_trunc('minute', new_end);

    PERFORM ${create_topic_segment_function}(topic.topic_name, topic.topic_seq_id, new_start, new_end);
    
    created_segment_start = new_start;
    created_segment_end = new_end;
    RETURN NEXT;
    RETURN;
END;
$$;