using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Npgsql;
using Topiqueue.Core.Dao;
using Topiqueue.Core.Messages.Models;
using Topiqueue.Postgres.Configuration;
using Topiqueue.Postgres.Helpers;

namespace Topiqueue.Postgres.Dao;

internal class PgsqlProducerDao : ITpqProducerDao
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly string _insertQuery;

    public PgsqlProducerDao(NpgsqlDataSource dataSource, TpqPostgresSettings settings)
    {
        _dataSource = dataSource;
        
        _insertQuery = @$"
            INSERT INTO {DbNames.MessageTable(settings)} (
                topic_name,
                partition_num,
                partition_key,
                message_type,
                data_txt
            )
            VALUES ($1, $2, $3, $4, $5)
        ";
    }
    
    public void Insert(TpqCreateMessageModel message)
    {
        using var conn = _dataSource.OpenConnection();
        using var cmd = CreateInsertCommand(conn, message);
        cmd.ExecuteNonQuery();
    }

    public async Task InsertAsync(TpqCreateMessageModel message)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = CreateInsertCommand(conn, message);
        await cmd.ExecuteNonQueryAsync();
    }

    public void InsertBatch(IReadOnlyList<TpqCreateMessageModel> messages)
    {
        using var conn = _dataSource.OpenConnection();
        using var batch = new NpgsqlBatch(conn);
        foreach (var message in messages)
        {
            var cmd = CreateInsertCommandForBatch(message);
            batch.BatchCommands.Add(cmd);
        }

        batch.ExecuteNonQuery();
    }

    public async Task InsertBatchAsync(IReadOnlyList<TpqCreateMessageModel> messages)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var batch = new NpgsqlBatch(conn);
        foreach (var message in messages)
        {
            var cmd = CreateInsertCommandForBatch(message);
            batch.BatchCommands.Add(cmd);
        }
        
        await batch.ExecuteNonQueryAsync();
    }

    private NpgsqlCommand CreateInsertCommand(NpgsqlConnection conn, TpqCreateMessageModel message)
    {
        var cmd = new NpgsqlCommand(_insertQuery, conn);
        cmd.Parameters.Add(new() { Value = message.TopicName });
        cmd.Parameters.Add(new() { Value = message.PartitionNum });
        cmd.Parameters.Add(new() { Value = (object?)message.PartitionKey ?? DBNull.Value });
        cmd.Parameters.Add(new() { Value = message.MessageType });
        cmd.Parameters.Add(new() { Value = (object?)message.DataTxt ?? DBNull.Value });
        return cmd;
    }

    private NpgsqlBatchCommand CreateInsertCommandForBatch(TpqCreateMessageModel message)
    {
        var cmd = new NpgsqlBatchCommand(_insertQuery);
        cmd.Parameters.Add(new() { Value = message.TopicName });
        cmd.Parameters.Add(new() { Value = message.PartitionNum });
        cmd.Parameters.Add(new() { Value = (object?)message.PartitionKey ?? DBNull.Value });
        cmd.Parameters.Add(new() { Value = message.MessageType });
        cmd.Parameters.Add(new() { Value = (object?)message.DataTxt ?? DBNull.Value });
        return cmd;
    }
}