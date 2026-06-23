using System.Threading.Tasks;
using Npgsql;
using Topiqueue.Core.Dao;
using Topiqueue.Core.Messages.Models;
using Topiqueue.Postgres.Configuration;
using Topiqueue.Postgres.Helpers;

namespace Topiqueue.Postgres.Dao;

internal class PgsqlMessagesDao : ITpqMessagesDao
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly string _insertQuery;

    public PgsqlMessagesDao(NpgsqlDataSource dataSource, TpqPostgresSettings settings)
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

    private NpgsqlCommand CreateInsertCommand(NpgsqlConnection conn, TpqCreateMessageModel message)
    {
        var cmd = new NpgsqlCommand(_insertQuery, conn);
        cmd.Parameters.Add(new() { Value = message.TopicName });
        cmd.Parameters.Add(new() { Value = message.PartitionNum });
        cmd.Parameters.Add(new() { Value = message.PartitionKey });
        cmd.Parameters.Add(new() { Value = message.MessageType });
        cmd.Parameters.Add(new() { Value = message.DataTxt });
        return cmd;
    }
}