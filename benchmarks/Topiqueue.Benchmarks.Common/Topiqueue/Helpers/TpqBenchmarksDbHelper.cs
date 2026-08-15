using Npgsql;

namespace Topiqueue.Benchmarks.Common.Topiqueue.Helpers;

public static class TpqBenchmarksDbHelper
{
    public static void DropAllTables(NpgsqlDataSource dataSource)
    {
        using var conn = dataSource.OpenConnection();
        
        using var batch = new NpgsqlBatch(conn);
        
        batch.BatchCommands.Add(new NpgsqlBatchCommand("DROP TABLE IF EXISTS tpq_topic_consumer"));
        batch.BatchCommands.Add(new NpgsqlBatchCommand("DROP TABLE IF EXISTS tpq_topic_segment"));
        batch.BatchCommands.Add(new NpgsqlBatchCommand("DROP TABLE IF EXISTS tpq_topic"));
        batch.BatchCommands.Add(new NpgsqlBatchCommand("DROP TABLE IF EXISTS tpq_server_consumer"));
        batch.BatchCommands.Add(new NpgsqlBatchCommand("DROP TABLE IF EXISTS tpq_server"));
        batch.BatchCommands.Add(new NpgsqlBatchCommand("DROP TABLE IF EXISTS tpq_message"));
        batch.BatchCommands.Add(new NpgsqlBatchCommand("DROP TABLE IF EXISTS tpq_evolve_migrations"));

        batch.ExecuteNonQuery();
    }
    
    public static void DeleteServer(NpgsqlDataSource dataSource, string serverId)
    {
        using var conn = dataSource.OpenConnection();
        using var cmd = new NpgsqlCommand("DELETE FROM tpq_server WHERE id = $1", conn);
        cmd.Parameters.Add(new() { Value = serverId });
        cmd.ExecuteNonQuery(); 
    } 
}