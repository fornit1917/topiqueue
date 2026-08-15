using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Topiqueue.Benchmarks.Common.Helpers;
using Topiqueue.Benchmarks.Common.Topiqueue.Helpers;
using Topiqueue.Benchmarks.Common.Topiqueue.Messages;
using Topiqueue.Core;
using Topiqueue.Core.Messages.Models;

namespace Topiqueue.Benchmarks.TopiqueueBenchmarks;

[BenchmarkCategory("Topiqueue", "InsertBatch")]
[MemoryDiagnoser]
[WarmupCount(5)]
[IterationCount(5)]
public class TpqInsertBatchBenchmark
{
    private TpqBenchmarksConfig? _cfg;
    private TpqServices? _tpq;

    private const int BatchSize = 10;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _cfg = new TpqBenchmarksConfig
        {
            DataSource = DataSourceFactory.Create()
        };
        _tpq = TpqBenchmarksInitializer.Init(_cfg);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _cfg?.DataSource.Dispose();
    }
    
    [Benchmark]
    public async Task TopiqueueInsertBatchMessages()
    {
        var batch = new List<TpqCreateMessageModel>(capacity: BatchSize);
        for (int i = 1; i <= BatchSize; i++)
        {
            var messageData = new TpqBenchmarksMessage
            {
                Id = 1,
                Value = Guid.NewGuid().ToString(),
                DelayMs = 0,
            };

            var partitionKey = i.ToString();
            var message = _tpq!.MessageFactory.Create(_cfg!.TopicName, messageData, partitionKey);
            batch.Add(message);
        }

        await _tpq!.Producer.ProduceBatchAsync(batch);
    }
}