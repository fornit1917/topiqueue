using Jobby.Core.Models;
using Npgsql;

namespace Topiqueue.Benchmarks.JobbyBenchmarks.Helpers;

public static class JobbyBenchmarksDbHelper
{
    public static void RemoveAllJobs(NpgsqlDataSource dataSource)
    {
        using var conn = dataSource.OpenConnection();
        using var cmd = dataSource.CreateCommand("DELETE FROM jobby_jobs");
        cmd.ExecuteNonQuery();
    }
}