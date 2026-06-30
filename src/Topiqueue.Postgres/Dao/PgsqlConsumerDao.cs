using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using Topiqueue.Core.Configuration;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Dao;
using Topiqueue.Core.Dao.Models;
using Topiqueue.Core.Exceptions;
using Topiqueue.Postgres.Configuration;
using Topiqueue.Postgres.Helpers;

namespace Topiqueue.Postgres.Dao;

internal class PgsqlConsumerDao : ITpqConsumerDao
{
    private readonly NpgsqlDataSource _dataSource;

    private readonly string _insertTopicConsumerQuery;
    private readonly string _getCapturedPartitionsCountQuery;
    private readonly string _capturePartitionsQuery;
    private readonly string _releasePartitionsQuery;

    public PgsqlConsumerDao(NpgsqlDataSource dataSource, TpqPostgresSettings settings)
    {
        _dataSource = dataSource;
        
        _insertTopicConsumerQuery = $@"
            INSERT INTO {DbNames.TopicConsumerTable(settings)} (topic_name, consumer_group_id, partition_num)
            VALUES ($1, $2, $3)
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
                var cmd = new NpgsqlBatchCommand(_insertTopicConsumerQuery);
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
                LastProcessedTxId = reader.GetString(reader.GetOrdinal("last_processed_tx_id")),
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
}