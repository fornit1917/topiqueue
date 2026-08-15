using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Topiqueue.Benchmarks.Common.Helpers;
using Topiqueue.Benchmarks.Common.Topiqueue.Helpers;
using Topiqueue.Benchmarks.Common.Topiqueue.Messages;
using Topiqueue.Core;

namespace Topiqueue.Benchmarks.TopiqueueBenchmarks;

[BenchmarkCategory("Topiqueue", "Insert")]
[MemoryDiagnoser]
[WarmupCount(5)]
[IterationCount(5)]
public class TpqInsertBenchmark
{
    private TpqBenchmarksConfig? _cfg;
    private TpqServices? _tpq;

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
    public async Task TopiqueueInsertMessage()
    {
        var message = new TpqBenchmarksMessage
        {
            Id = 1,
            Value = Guid.NewGuid().ToString(),
            DelayMs = 0,
        };
        
        await _tpq!.Producer.ProduceAsync(_cfg!.TopicName, message);
    }
}