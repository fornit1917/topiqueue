using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Jobby.Core.Interfaces;
using Jobby.Core.Models;
using Jobby.Core.Services;
using Jobby.Postgres.ConfigurationExtensions;
using Npgsql;
using Topiqueue.Benchmarks.Common.Helpers;
using Topiqueue.Benchmarks.JobbyBenchmarks.Commands;

namespace Topiqueue.Benchmarks.JobbyBenchmarks;

[BenchmarkCategory("Jobby", "InsertBatch")]
[MemoryDiagnoser]
[WarmupCount(5)]
[IterationCount(5)]
public class JobbyInsertBatchBenchmark
{
    private IJobbyClient? _jobbyClient;
    private NpgsqlDataSource? _dataSource;
    
    private const int BatchSize = 10;

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
    public async Task JobbyBatchInsertJobs()
    {
        var jobs = new List<JobCreationModel>(capacity: BatchSize);
        for (int i = 1; i <= BatchSize; i++)
        {
            var jobCommand = new JobbyBenchmarksCommand()
            {
                Id = i,
                Value = Guid.NewGuid().ToString(),
                DelayMs = 0,
            };
            jobs.Add(_jobbyClient!.Factory.Create(jobCommand, new JobOpts
            {
                SerializableGroupId = i.ToString()
            }));
        }
        await _jobbyClient!.EnqueueBatchAsync(jobs);
    }
}