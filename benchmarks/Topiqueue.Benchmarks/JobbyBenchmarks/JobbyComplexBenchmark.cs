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
using Topiqueue.Benchmarks.JobbyBenchmarks.Helpers;
using Topiqueue.Benchmarks.JobbyBenchmarks.Services;

namespace Topiqueue.Benchmarks.JobbyBenchmarks;

[BenchmarkCategory("Jobby", "Complex")]
[WarmupCount(2)]
[IterationCount(5)]
[ProcessCount(1)]
[InvocationCount(1)]
[MemoryDiagnoser]
public class JobbyComplexBenchmark
{
    private NpgsqlDataSource? _dataSource;
    private IJobbyServer? _jobbyServer;
    private IJobbyClient? _jobbyClient;

    private const int JobsCount = 1000;
    private const int InsertBatchSize = 200;
    
    [Params(1, 10)]
    public int ParallelismDegree { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _dataSource = DataSourceFactory.Create();
        
        JobbyBenchmarksDbHelper.RemoveAllJobs(_dataSource);
        
        var builder = new JobbyBuilder();
        builder
            .UsePostgresql(_dataSource)
            .UseExecutionScopeFactory(new JobbyTestExecutionScopeFactory())
            .UseServerSettings(new JobbyServerSettings
            {
                PollingIntervalMs = 100,
                CompleteWithBatching = true,
                MaxDegreeOfParallelism = ParallelismDegree,
            });

        builder.AddJob<JobbyBenchmarksCommand, JobbyBenchmarksCommandHandler>();
        
        var migrator = builder.CreateStorageMigrator();
        migrator.Migrate();
        
        _jobbyClient = builder.CreateJobbyClient();
        _jobbyServer = builder.CreateJobbyServer();
        _jobbyServer.StartBackgroundService();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _jobbyServer!.SendStopSignal();
        while (_jobbyServer.HasInProgressJobs())
        {
            Thread.Sleep(100);
        }
    }
    
    [IterationSetup]
    public void IterationSetup()
    {
        Counter.Reset(JobsCount);
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        Thread.Sleep(3000);
    }

    [Benchmark]
    public void JobbyInsertAndHandleJobs()
    {
        Counter.Reset(JobsCount);
        
        _ = Task.Run(() =>
        {
            var batch = new List<JobCreationModel>(capacity: InsertBatchSize);
            for (int i = 0; i < JobsCount; i++)
            {
                var jobCommand = new JobbyBenchmarksCommand()
                {
                    Id = i,
                    Value = Guid.NewGuid().ToString(),
                    DelayMs = 0,
                };
                batch.Add(_jobbyClient!.Factory.Create(jobCommand, new JobOpts
                {
                    SerializableGroupId = (i % 100).ToString(),
                }));
                
                if (batch.Count == InsertBatchSize)
                {
                    _jobbyClient!.EnqueueBatch(batch);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                _jobbyClient!.EnqueueBatch(batch);
                batch.Clear();
            }
        });
        
        Counter.Task.GetAwaiter().GetResult();
    }
}