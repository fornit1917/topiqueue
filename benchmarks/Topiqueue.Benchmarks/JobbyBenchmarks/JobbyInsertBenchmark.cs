using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Jobby.Core.Interfaces;
using Jobby.Core.Services;
using Jobby.Postgres.ConfigurationExtensions;
using Npgsql;
using Topiqueue.Benchmarks.Common.Helpers;
using Topiqueue.Benchmarks.JobbyBenchmarks.Commands;

namespace Topiqueue.Benchmarks.JobbyBenchmarks;

[BenchmarkCategory("Jobby", "Insert")]
[MemoryDiagnoser]
[WarmupCount(5)]
[IterationCount(5)]
public class JobbyInsertBenchmark
{
    private IJobbyClient? _jobbyClient;

    private NpgsqlDataSource? _dataSource;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _dataSource = DataSourceFactory.Create();
        var builder = new JobbyBuilder();
        builder.UsePostgresql(_dataSource);
        
        var migrator = builder.CreateStorageMigrator();
        migrator.Migrate();
        
        _jobbyClient = builder.CreateJobbyClient();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _dataSource?.Dispose();
    }

    [Benchmark]
    public async Task JobbyInsertJob()
    {
        var jobCommand = new JobbyBenchmarksCommand()
        {
            Id = 1,
            Value = Guid.NewGuid().ToString(),
            DelayMs = 0,
        };
        await _jobbyClient!.EnqueueCommandAsync(jobCommand);
    }
}