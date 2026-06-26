using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Dao;
using Topiqueue.Core.Exceptions;
using Topiqueue.Postgres.Configuration;
using Topiqueue.Postgres.Helpers;

namespace Topiqueue.Postgres.Dao;

internal class PgsqlServersDao : ITpqServersDao
{
    private readonly NpgsqlDataSource _dataSource;
    
    private readonly string _insertServerQuery;
    private readonly string _insertServerConsumersQuery;
    private readonly string _deleteOutdatedQuery;
    private readonly string _updateHeartbeatTsQuery;
    private readonly string _getServersCountInGroupQuery;

    public PgsqlServersDao(NpgsqlDataSource dataSource, TpqPostgresSettings settings)
    {
        _dataSource = dataSource;
        
        _insertServerQuery = $@"
            INSERT INTO {DbNames.ServerTable(settings)} (id) VALUES ($1)
        ";

        _insertServerConsumersQuery = $@"
            INSERT INTO {DbNames.ServerConsumerTable(settings)} (server_id, topic_name, consumer_group_id)
            VALUES ($1, $2, $3)
        ";

        _deleteOutdatedQuery = $@"
            DELETE FROM {DbNames.ServerTable(settings)}
            WHERE id IN (
                SELECT id FROM {DbNames.ServerTable(settings)}
                WHERE heartbeat_ts < now() - $1
                FOR UPDATE SKIP LOCKED
            )
            RETURNING id
        ";

        _updateHeartbeatTsQuery = $@"UPDATE {DbNames.ServerTable(settings)} SET heartbeat_ts = now() WHERE id = $1";

        _getServersCountInGroupQuery = $@"
            SELECT count(1) FROM {DbNames.ServerConsumerTable(settings)} sc
            INNER JOIN {DbNames.ServerTable(settings)} s ON s.id = sc.server_id
            WHERE 
                sc.topic_name = $1
                AND sc.consumer_group_id = $2
                AND s.heartbeat_ts >= now() - $3
        ";
    }

    public void AnnounceServer(string serverId, IReadOnlyList<TpqConsumerSettings> consumers)
    {
        using var conn = _dataSource.OpenConnection();
        using var tx = conn.BeginTransaction();
        
        using var insertServerCmd = new NpgsqlCommand(_insertServerQuery, conn, tx);
        insertServerCmd.Parameters.Add(new() { Value = serverId });
        insertServerCmd.ExecuteNonQuery();

        using var insertServerConsumersBatch = new NpgsqlBatch(conn, tx);
        foreach (var consumer in consumers)
        {
            var insertServerConsumerCmd = new NpgsqlBatchCommand(_insertServerConsumersQuery);
            insertServerConsumerCmd.Parameters.Add(new() { Value = serverId });
            insertServerConsumerCmd.Parameters.Add(new() { Value = consumer.TopicName });
            insertServerConsumerCmd.Parameters.Add(new() { Value = consumer.ConsumerGroupId });
            insertServerConsumersBatch.BatchCommands.Add(insertServerConsumerCmd);
        }
        insertServerConsumersBatch.ExecuteNonQuery();

        tx.Commit();
    }

    public void DeleteOutdated(TimeSpan threshold, List<string> deletedServerIds)
    {
        deletedServerIds.Clear();
        
        using var conn = _dataSource.OpenConnection();
        using var cmd =  new NpgsqlCommand(_deleteOutdatedQuery, conn);
        cmd.Parameters.Add(new() { Value = threshold });
        
        using var reader = cmd.ExecuteReader();
        if (!reader.HasRows)
        {
            return;
        }

        while (reader.Read())
        {
            var deletedServerId = reader.GetString(0);
            deletedServerIds.Add(deletedServerId);
        }
    }

    public async Task DeleteOutdatedAsync(TimeSpan threshold, List<string> deletedServerIds)
    {
        deletedServerIds.Clear();

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd =  new NpgsqlCommand(_deleteOutdatedQuery, conn);
        cmd.Parameters.Add(new() { Value = threshold });

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!reader.HasRows)
        {
            return;
        }

        while (reader.Read())
        {
            var deletedServerId = reader.GetString(0);
            deletedServerIds.Add(deletedServerId);
        }
    }

    public async Task UpsertHeartbeatTsAsync(string serverId, IReadOnlyList<TpqConsumerSettings> consumers)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd =  new NpgsqlCommand(_updateHeartbeatTsQuery, conn);
        cmd.Parameters.Add(new() { Value = serverId });
        var updatedCount = await cmd.ExecuteNonQueryAsync();
        if (updatedCount == 0)
        {
            await AnnounceServerAsync(serverId, consumers);
        }
    }

    public async Task<int> GetServersCountInGroupAsync(string topicName, string consumerGroupId, TimeSpan outdatedThreshold)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(_getServersCountInGroupQuery, conn);
        cmd.Parameters.Add(new() { Value = topicName });
        cmd.Parameters.Add(new() { Value = consumerGroupId });
        cmd.Parameters.Add(new() { Value = outdatedThreshold });
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!reader.HasRows || !await reader.ReadAsync())
        {
            throw new UnexpectedDbResultException("Could not get servers count for group");
        }
        var count = reader.GetInt32(0);
        return count;
    }

    private async Task AnnounceServerAsync(string serverId, IReadOnlyList<TpqConsumerSettings> consumers)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();

        await using var insertServerCmd = new NpgsqlCommand(_insertServerQuery, conn, tx);
        insertServerCmd.Parameters.Add(new() { Value = serverId });
        insertServerCmd.ExecuteNonQuery();

        await using var insertServerConsumersBatch = new NpgsqlBatch(conn, tx);
        foreach (var consumer in consumers)
        {
            var insertServerConsumerCmd = new NpgsqlBatchCommand(_insertServerConsumersQuery);
            insertServerConsumerCmd.Parameters.Add(new() { Value = serverId });
            insertServerConsumerCmd.Parameters.Add(new() { Value = consumer.TopicName });
            insertServerConsumerCmd.Parameters.Add(new() { Value = consumer.ConsumerGroupId });
            insertServerConsumersBatch.BatchCommands.Add(insertServerConsumerCmd);
        }
        await insertServerConsumersBatch.ExecuteNonQueryAsync();

        await tx.CommitAsync();
    }    
}