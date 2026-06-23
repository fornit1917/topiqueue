using System;
using Microsoft.Extensions.Logging;
using Npgsql;
using Topiqueue.Core;
using Topiqueue.Core.Configuration;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Postgres.Configuration;
using Topiqueue.Samples.Cli.Common.Messages;

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
            .UsePostgresql(dataSource)
            .UseBackgroundServiceSettings(new TpqBackgroundServiceSettings
            {
                RotateSegmentsInterval = TimeSpan.FromSeconds(5),
            })
            .UseTopics([
                new TpqTopicSettings("topic_1", 8, TimeSpan.FromHours(1)),
                new TpqTopicSettings("topic_2", 4, TimeSpan.FromDays(7)),
            ]);
        
        var tpq = new TpqServices(tpqConfig);
        tpq.Initializer.Initialize();
        tpq.BackgroundService.StartBackgroundService();

        for (int i = 1; i <= 20; i++)
        {
            var message = new DemoMessageData
            {
                Id = i,
                Value = $"Value {i}"
            };
            var partitionKey = $"key_{i}"; 
            tpq.Producer.Produce("topic_1", message, partitionKey);            
        }
        Console.WriteLine("Produced 20 messages");
        
        Console.ReadLine();
    }
}