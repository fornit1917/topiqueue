using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using Topiqueue.Core.Configuration;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Dao;
using Topiqueue.Postgres.Configuration;
using Topiqueue.Postgres.Helpers;

namespace Topiqueue.Postgres.Dao;

internal class PgsqlConsumerDao : ITpqConsumerDao
{
    private readonly NpgsqlDataSource _dataSource;

    private readonly string _insertTopicConsumerQuery;
    private readonly string _tryCapturePartitionsQuery;
    private readonly string _tryReleasePartitionsQuery;

    public PgsqlConsumerDao(NpgsqlDataSource dataSource, TpqPostgresSettings settings)
    {
        _dataSource = dataSource;
        
        _insertTopicConsumerQuery = $@"
            INSERT INTO {DbNames.TopicConsumerTable(settings)} (topic_name, consumer_group_id, partition_num)
            VALUES ($1, $2, $3)
            ON CONFLICT (topic_name, consumer_group_id, partition_num) DO NOTHING
        ";

        _tryCapturePartitionsQuery = @$"
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
            RETURNING partition_num
        ";

        _tryReleasePartitionsQuery = @$"
            UPDATE {DbNames.TopicConsumerTable(settings)}
            SET server_id = NULL
            WHERE (topic_name, consumer_group_id, partition_num) IN (
                SELECT topic_name, consumer_group_id, partition_num
                FROM {DbNames.TopicConsumerTable(settings)} x
                WHERE
                    x.server_id = $1 
                    AND x.topic_name = $2
                    AND x.consumer_group_id = $3
                ORDER BY partition_num
                LIMIT $4
                FOR UPDATE SKIP LOCKED
            )
            RETURNING partition_num            
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

    public async Task TryCapturePartitionsAsync(string serverId, TpqConsumerSettings consumer, int partitionCount, List<int> capturedPartitions)
    {
        capturedPartitions.Clear();
        
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(_tryCapturePartitionsQuery, conn);
        cmd.Parameters.Add(new() { Value = serverId });
        cmd.Parameters.Add(new() { Value = consumer.TopicName });
        cmd.Parameters.Add(new() { Value = consumer.ConsumerGroupId });
        cmd.Parameters.Add(new() { Value = partitionCount });
        
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!reader.HasRows)
        {
            return;
        }

        while (await reader.ReadAsync())
        {
            capturedPartitions.Add(reader.GetInt32(0));
        }
    }

    public async Task TryReleasePartitionsAsync(string serverId, TpqConsumerSettings consumer, int partitionCount,
        List<int> releasedPartitions)
    {
        releasedPartitions.Clear();
        
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(_tryReleasePartitionsQuery, conn);
        cmd.Parameters.Add(new() { Value = serverId });
        cmd.Parameters.Add(new() { Value = consumer.TopicName });
        cmd.Parameters.Add(new() { Value = consumer.ConsumerGroupId });
        cmd.Parameters.Add(new() { Value = partitionCount });
        
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!reader.HasRows)
        {
            return;
        }

        while (await reader.ReadAsync())
        {
            releasedPartitions.Add(reader.GetInt32(0));
        }    
    }
}