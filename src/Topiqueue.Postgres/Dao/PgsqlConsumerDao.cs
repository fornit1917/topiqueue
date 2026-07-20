using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using Topiqueue.Core.Configuration;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Dao;
using Topiqueue.Core.Dao.Models;
using Topiqueue.Core.Exceptions;
using Topiqueue.Core.Messages.Models;
using Topiqueue.Postgres.Configuration;
using Topiqueue.Postgres.Helpers;

namespace Topiqueue.Postgres.Dao;

internal class PgsqlConsumerDao : ITpqConsumerDao
{
    private readonly NpgsqlDataSource _dataSource;

    private readonly string _insertTopicConsumerQueryEarliestOffset;
    private readonly string _insertTopicConsumerQueryLatestOffset;
    private readonly string _getCapturedPartitionsCountQuery;
    private readonly string _capturePartitionsQuery;
    private readonly string _releasePartitionsQuery;
    private readonly string _readMessagesQuery;
    private readonly string _commitOffsetQuery;

    public PgsqlConsumerDao(NpgsqlDataSource dataSource, TpqPostgresSettings settings)
    {
        _dataSource = dataSource;
        
        _insertTopicConsumerQueryEarliestOffset = $@"
            INSERT INTO {DbNames.TopicConsumerTable(settings)} (topic_name, consumer_group_id, partition_num)
            VALUES ($1, $2, $3)
            ON CONFLICT (topic_name, consumer_group_id, partition_num) DO NOTHING
        ";

        _insertTopicConsumerQueryLatestOffset = $@"
            INSERT INTO {DbNames.TopicConsumerTable(settings)} 
                (topic_name, consumer_group_id, partition_num, last_processed_tx_id, last_processed_seq_id, last_processed_created_at)
            SELECT tc.topic_name, tc.consumer_group_id, tc.partition_num, tc.tx_id, tc.seq_id, tc.created_at
            FROM (
                (
                    SELECT 
                        $1 as topic_name, 
                        $2 as consumer_group_id, 
                        $3 as partition_num,
                        tx_id::text::bigint, 
                        seq_id, 
                        created_at
                    FROM {DbNames.MessageTable(settings)}
                    WHERE 
                        topic_name = $1 
                        AND partition_num = $3
                    ORDER BY (tx_id, seq_id) DESC 
                    LIMIT 1
                )  
                UNION ALL
                (
                    SELECT
                        $1 as topic_name, 
                        $2 as consumer_group_id,
                        $3 as partition_num,
                        0::bigint, 
                        0::bigint, 
                        '0001-01-01T12:00:00Z'::timestamptz    
                )
            ) as tc
            LIMIT 1
            ON CONFLICT (topic_name, consumer_group_id, partition_num) DO NOTHING
        ";

        _getCapturedPartitionsCountQuery = $@"
            SELECT COUNT(1) FROM {DbNames.TopicConsumerTable(settings)}
            WHERE 
                server_id = $1
                AND topic_name = $2
                AND consumer_group_id = $3
        ";

        _capturePartitionsQuery = @$"
            UPDATE {DbNames.TopicConsumerTable(settings)}
            SET server_id = $1
            WHERE (topic_name, consumer_group_id, partition_num) IN (
                SELECT topic_name, consumer_group_id, partition_num
                FROM {DbNames.TopicConsumerTable(settings)} x
                WHERE
                    x.server_id IS NULL 
                    AND x.topic_name = $2
                    AND x.consumer_group_id = $3
                ORDER BY partition_num
                LIMIT $4
                FOR UPDATE SKIP LOCKED
            )
            RETURNING partition_num, last_processed_tx_id, last_processed_seq_id, last_processed_created_at
        ";

        _releasePartitionsQuery = @$"
            UPDATE {DbNames.TopicConsumerTable(settings)}
            SET server_id = NULL
            WHERE (topic_name, consumer_group_id, partition_num) IN (
                SELECT topic_name, consumer_group_id, partition_num
                FROM {DbNames.TopicConsumerTable(settings)} x
                WHERE
                    x.server_id = $1 
                    AND x.topic_name = $2
                    AND x.consumer_group_id = $3
                    AND x.partition_num = ANY($4)
                FOR UPDATE SKIP LOCKED
            )
        ";

        _readMessagesQuery = $@"
            SELECT tx_id::text::bigint, seq_id, created_at, partition_key, message_type, data_txt 
            FROM {DbNames.MessageTable(settings)}
            WHERE
                topic_name = $1
                AND partition_num = $2
                AND (tx_id, seq_id) > ($3::text::xid8, $4)
                AND tx_id < pg_snapshot_xmin(pg_current_snapshot())
                AND created_at >= $5
                AND created_at <= now()
            ORDER BY
                (tx_id, seq_id) ASC
            LIMIT $6
        ";

        _commitOffsetQuery = $@"
            UPDATE {DbNames.TopicConsumerTable(settings)}
            SET
                last_processed_tx_id = $1,
                last_processed_seq_id = $2,
                last_processed_created_at = $3
            WHERE
                topic_name = $4
                AND consumer_group_id = $5
                AND partition_num = $6
                AND server_id = $7
        ";
    }

    public void AnnounceConsumers(IReadOnlyList<TpqConsumerSettings> consumers, ITopicsRegistry topicsRegistry)
    {
        using var conn = _dataSource.OpenConnection();
        using var batch = new NpgsqlBatch(conn);
        foreach (var consumer in consumers)
        {
            var topic = topicsRegistry.Get(consumer.TopicName);
            for (int i = 0; i < topic.PartitionsCount; i++)
            {
                var insertQuery = consumer.AutoResetOffset == TpqAutoResetOffset.Earliest
                    ? _insertTopicConsumerQueryEarliestOffset
                    : _insertTopicConsumerQueryLatestOffset;
                
                var cmd = new NpgsqlBatchCommand(insertQuery);
                cmd.Parameters.Add(new() { Value = consumer.TopicName });
                cmd.Parameters.Add(new() { Value = consumer.ConsumerGroupId });
                cmd.Parameters.Add(new() { Value = i });
                batch.BatchCommands.Add(cmd);
            }    
        }
        
        batch.ExecuteNonQuery();
    }

    public async Task<int> GetCapturedPartitionsCount(string serverId, TpqConsumerSettings consumer)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(_getCapturedPartitionsCountQuery, conn);
        cmd.Parameters.Add(new() { Value = serverId });
        cmd.Parameters.Add(new() { Value = consumer.TopicName });
        cmd.Parameters.Add(new() { Value = consumer.ConsumerGroupId });
        
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!reader.HasRows || !reader.Read())
        {
            throw new UnexpectedDbResultException("Could not get captured partitions count");
        }

        return reader.GetInt32(0);
    }

    public async Task<List<CapturedPartition>> CapturePartitionsAsync(string serverId, TpqConsumerSettings consumer, int partitionCount)
    {
        var result = new List<CapturedPartition>(capacity: partitionCount);
        
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(_capturePartitionsQuery, conn);
        cmd.Parameters.Add(new() { Value = serverId });
        cmd.Parameters.Add(new() { Value = consumer.TopicName });
        cmd.Parameters.Add(new() { Value = consumer.ConsumerGroupId });
        cmd.Parameters.Add(new() { Value = partitionCount });
        
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!reader.HasRows)
        {
            return result;
        }

        while (await reader.ReadAsync())
        {
            var capturedPartition = new CapturedPartition
            {
                PartitionNum = reader.GetInt32(reader.GetOrdinal("partition_num")),
                LastProcessedTxId = reader.GetInt64(reader.GetOrdinal("last_processed_tx_id")),
                LastProcessedSeqId = reader.GetInt64(reader.GetOrdinal("last_processed_seq_id")),
                LastProcessedCreatedAt = reader.GetDateTime(reader.GetOrdinal("last_processed_created_at")),
            };
            result.Add(capturedPartition);
        }

        return result;
    }

    public async Task ReleasePartitionsAsync(string serverId, TpqConsumerSettings consumer, IReadOnlyList<int> partitionNums)
    {
        if (partitionNums.Count == 0)
        {
            return;
        }
        
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(_releasePartitionsQuery, conn);
        cmd.Parameters.Add(new() { Value = serverId });
        cmd.Parameters.Add(new() { Value = consumer.TopicName });
        cmd.Parameters.Add(new() { Value = consumer.ConsumerGroupId });
        cmd.Parameters.Add(new() { Value = partitionNums });

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ReadMessagesAsync(ReadMessagesRequest request, List<TpqMessageModel> result)
    {
        result.Clear();
        
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(_readMessagesQuery, conn);
        cmd.Parameters.Add(new() { Value = request.TopicName });
        cmd.Parameters.Add(new() { Value = request.PartitionNum });
        cmd.Parameters.Add(new() { Value = request.Offset.TxId });
        cmd.Parameters.Add(new() { Value = request.Offset.SeqId });
        cmd.Parameters.Add(new() { Value = request.Offset.CreatedAt.AddHours(-1) });
        cmd.Parameters.Add(new() { Value = request.Limit });
        
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!reader.HasRows)
        {
            return;
        }

        while (await reader.ReadAsync())
        {
            var message = new TpqMessageModel
            {
                TopicName = request.TopicName,
                PartitionNum = request.PartitionNum,
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                PartitionKey = reader.GetNullableString("partition_key"),
                MessageType = reader.GetString(reader.GetOrdinal("message_type")),
                SeqId = reader.GetInt64(reader.GetOrdinal("seq_id")),
                TxId = reader.GetInt64(reader.GetOrdinal("tx_id")),
                DataTxt = reader.GetNullableString("data_txt"),
            };
            result.Add(message);
        }
    }

    public async Task<bool> CommitOffsetAsync(string serverId, TpqConsumerSettings consumer, int partitionNum, PartitionOffset offset)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(_commitOffsetQuery, conn);
        cmd.Parameters.Add(new() { Value = offset.TxId });
        cmd.Parameters.Add(new() { Value = offset.SeqId });
        cmd.Parameters.Add(new() { Value = offset.CreatedAt });
        cmd.Parameters.Add(new () { Value = consumer.TopicName });
        cmd.Parameters.Add(new() { Value = consumer.ConsumerGroupId });
        cmd.Parameters.Add(new() { Value = partitionNum });
        cmd.Parameters.Add(new() { Value = serverId });
        
        var updatedCount = await cmd.ExecuteNonQueryAsync();

        return updatedCount > 0;
    }
}