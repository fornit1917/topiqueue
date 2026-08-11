using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Npgsql;
using Topiqueue.Core;
using Topiqueue.Core.Configuration;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Messages.Models;
using Topiqueue.Postgres.Configuration;
using Topiqueue.Samples.Cli.Common.Messages;
using Topiqueue.Samples.Cli.Common.ServiceContainer;

namespace Topiqueue.Samples.Cli.Common;

public static class Program
{
    public static void Main(string[] args)
    {
        var connectionString = "Host=localhost;Username=test_user;Password=12345;Database=test_db;GSS Encryption Mode=Disable";
        using var dataSource = NpgsqlDataSource.Create(connectionString);
        
        var loggerFactory = LoggerFactory.Create(x => x.AddConsole());

        var tpqConfig = new TpqConfig()
            .UseLoggerFactory(loggerFactory)
            .UseServiceContainerScopeFactory(new SampleCliServiceContainerScopeFactory())
            .UsePostgresql(dataSource)
            .UseBatchHandler<DemoMessageData, DemoMessageHandler>()
            .UseBackgroundServiceSettings(new TpqBackgroundServiceSettings
            {
                RotateSegmentsInterval = TimeSpan.FromSeconds(5),
                HeartbeatInterval = TimeSpan.FromSeconds(5),
                HeartbeatOutdatedThreshold = TimeSpan.FromSeconds(10),
                DbQueryExecutorWorkers = 2,
                MessagesHandlerWorkers = 2,
            })
            .UseTopics([
                new TpqTopicSettings("topic_1", 2, TimeSpan.FromHours(1)),
            ])
            .UseConsumers([
                new TpqConsumerSettings
                {
                    TopicName = "topic_1",
                    ConsumerGroupId = "topic_1_consumer_1",
                    TryCapturePartitionsOnStart = 4,
                    AutoResetOffset = TpqAutoResetOffset.Latest,
                    ReaderBatchSize = 5,
                    HandlerBatchSize = 1,
                    EmptyTopicPause = TimeSpan.FromSeconds(1),
                }
            ]);
        
        var tpq = new TpqServices(tpqConfig);
        tpq.Initializer.Initialize();

        while (true)
        {
            Console.WriteLine("Choose action:");
            Console.WriteLine("0. Exit");
            Console.WriteLine("1. Produce messages");
            Console.WriteLine("2. Start Topiqueue background service");

            var action = Console.ReadLine();

            if (action == "0")
            {
                break;
            }
            
            if (action == "1")
            {
                const int messagesCount = 20;
                var messages = new List<TpqCreateMessageModel>(capacity: messagesCount);
                for (int i = 1; i <= messagesCount; i++)
                {
                    var messageData = new DemoMessageData
                    {
                        Id = i,
                        Value = $"Value {i}"
                    };
                    var partitionKey = $"key_{i}";
                    var message = tpq.MessageFactory.Create("topic_1", messageData, partitionKey);
                    messages.Add(message);
                }
                
                tpq.Producer.ProduceBatch(messages);
                
                Console.WriteLine("Messages produced");
            }

            if (action == "2")
            {
                Console.WriteLine("Topiqueue service start... press enter to continue...");
                tpq.BackgroundService.StartBackgroundService();
                Console.ReadLine();
            }
            
            Console.WriteLine();
        }
    }
}