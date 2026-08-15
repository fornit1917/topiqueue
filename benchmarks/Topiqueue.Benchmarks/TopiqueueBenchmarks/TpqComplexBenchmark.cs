using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Npgsql;
using Topiqueue.Benchmarks.Common.Helpers;
using Topiqueue.Benchmarks.Common.Topiqueue.Helpers;
using Topiqueue.Benchmarks.Common.Topiqueue.Messages;
using Topiqueue.Core;
using Topiqueue.Core.Messages.Models;

namespace Topiqueue.Benchmarks.TopiqueueBenchmarks;

[BenchmarkCategory("Topiqueue", "Complex")]
[WarmupCount(2)]
[IterationCount(5)]
[ProcessCount(1)]
[InvocationCount(1)]
[MemoryDiagnoser]
public class TpqComplexBenchmark
{
    private TpqBenchmarksConfig? _cfg;
    private TpqServices? _tpq;

    private NpgsqlDataSource? _dataSource;
    
    private const int MessagesCount = 1000;
    private const int InsertBatchSize = 200;

    [Params(1, 10)]
    public int ParallelismDegree { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _dataSource = DataSourceFactory.Create();
        _cfg = new TpqBenchmarksConfig
        {
            DataSource = _dataSource!,
            PartitionsCount = ParallelismDegree,
            
            HandlerBatchSize = 1,
            ReaderBatchSize = 100,
            
            DbWorkers = ParallelismDegree < 2 ? 2 : ParallelismDegree,
            HandlerWorkers = ParallelismDegree,
        };
        _tpq = TpqBenchmarksInitializer.Init(_cfg);
        _tpq!.BackgroundService.StartBackgroundService();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _tpq!.BackgroundService.SendStopSignal();
        Thread.Sleep(2000);
        _dataSource?.Dispose();
    }
    
    [IterationSetup]
    public void IterationSetup()
    {
        Counter.Reset(MessagesCount);
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        Thread.Sleep(3000);
    }
    
    [Benchmark]
    public void TopiqueueProduceAndHandleMessages()
    {
        Counter.Reset(MessagesCount);
        
        Task.Run(() =>
        {
            var batch = new List<TpqCreateMessageModel>(capacity: InsertBatchSize);
            for (int i = 0; i < MessagesCount; i++)
            {
                var messageData = new TpqBenchmarksMessage
                {
                    Id = 1,
                    Value = Guid.NewGuid().ToString(),
                    DelayMs = 0,
                };

                var partitionKey = (i % 100).ToString();
                var message = _tpq!.MessageFactory.Create(_cfg!.TopicName, messageData, partitionKey);
                batch.Add(message);

                if (batch.Count == InsertBatchSize)
                {
                    _tpq!.Producer.ProduceBatch(batch);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                _tpq!.Producer.ProduceBatch(batch);
                batch.Clear();
            }
        });
        
        Counter.Task.GetAwaiter().GetResult();
    }
}