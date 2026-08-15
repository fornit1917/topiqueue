using System;
using Topiqueue.Benchmarks.Common.Topiqueue.Messages;
using Topiqueue.Benchmarks.Common.Topiqueue.Services;
using Topiqueue.Core;
using Topiqueue.Core.Configuration;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Postgres.Configuration;

namespace Topiqueue.Benchmarks.Common.Topiqueue.Helpers;

public static class TpqBenchmarksInitializer
{
    public static TpqServices Init(TpqBenchmarksConfig benchmarksConfig, bool initDb = true)
    {
        if (initDb)
            TpqBenchmarksDbHelper.DropAllTables(benchmarksConfig.DataSource);

        var tpqConfig = new TpqConfig()
            .UseServiceContainerScopeFactory(new TpqBenchmarksServicesScopeFactory())
            .UsePostgresql(benchmarksConfig.DataSource)
            .UseBatchHandler<TpqBenchmarksMessage, TpqBenchmarksMessageHandler>()
            .UseBackgroundServiceSettings(new TpqBackgroundServiceSettings
            {
                DbQueryExecutorWorkers = benchmarksConfig.DbWorkers,
                MessagesHandlerWorkers = benchmarksConfig.HandlerWorkers,
            })
            .UseTopics([
                new TpqTopicSettings(benchmarksConfig.TopicName, benchmarksConfig.PartitionsCount, TimeSpan.FromDays(1)),
            ])
            .UseConsumers([
                new TpqConsumerSettings
                {
                    TopicName = benchmarksConfig.TopicName,
                    ConsumerGroupId = $"{benchmarksConfig.TopicName}_consumer",
                    TryCapturePartitionsOnStart = benchmarksConfig.PartitionsCount,
                    AutoResetOffset = TpqAutoResetOffset.Latest,
                    ReaderBatchSize = benchmarksConfig.ReaderBatchSize,
                    HandlerBatchSize = benchmarksConfig.HandlerBatchSize,
                    EmptyTopicPause = benchmarksConfig.EmptyTopicPause,
                }
            ]);
        
        var tpq = new TpqServices(tpqConfig);
        
        if (initDb)
            tpq.Initializer.Initialize();

        return tpq;
    }
}