using System;
using System.Collections.Generic;
using System.Threading;
using Topiqueue.Benchmarks.Common.Helpers;
using Topiqueue.Benchmarks.Common.Topiqueue.Helpers;
using Topiqueue.Benchmarks.Common.Topiqueue.Messages;
using Topiqueue.Core;
using Topiqueue.Core.Messages.Models;

namespace Topiqueue.Benchmarks.Simple;

public static class Program
{
    const int BatchSize = 1000;
    
    public static void Main(string[] args)
    {
        var dataSource = DataSourceFactory.Create();
        var benchmarkConfig = new TpqBenchmarksConfig()
        {
            DataSource = dataSource,
            
            TopicName = "topic_b",
            PartitionsCount = 1,
            
            HandlerBatchSize = 1,
            ReaderBatchSize = 100,
            
            DbWorkers = 10,
            HandlerWorkers = 10,
        };
        
        var tpq = TpqBenchmarksInitializer.Init(benchmarkConfig, initDb: true);
        tpq.BackgroundService.StartBackgroundService();
        Thread.Sleep(3000);
        
        Console.WriteLine("Warm Up...");
        Run(benchmarkConfig, tpq);

        for (int i = 1; i <= 10; i++)
        {
            Console.WriteLine("------------------------------");
            Console.WriteLine($"Run {i}...");
            Run(benchmarkConfig, tpq);            
        }
    }

    private static void Run(TpqBenchmarksConfig benchmarkConfig, TpqServices tpq)
    {
        Counter.Reset(BatchSize);
        
        var batch = new List<TpqCreateMessageModel>(BatchSize);
        for (int i = 0; i < BatchSize; i++)
        {
            var partitionKey = i.ToString();
            var messageData = new TpqBenchmarksMessage
            {
                Id = i,
                Value = $"Message for benchmark {partitionKey}",
                DelayMs = 0
            };
            var message = tpq.MessageFactory.Create(benchmarkConfig.TopicName, messageData, partitionKey);
            batch.Add(message);
        }
        tpq.Producer.ProduceBatch(batch);
        
        Counter.StartTimer();
        
        Counter.Task.GetAwaiter().GetResult();
        
        Console.WriteLine("Handle messages " + Counter.GetElapsedTime().TotalMilliseconds + " ms");
        Thread.Sleep(3000);
    }
}