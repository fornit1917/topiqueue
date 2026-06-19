using System;
using Microsoft.Extensions.Logging;
using Npgsql;
using Topiqueue.Core;
using Topiqueue.Core.Configuration;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Postgres.Configuration;

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
            .UseTopics([
                new TpqTopicSettings("topic_1", 8, TimeSpan.FromHours(1)),
                new TpqTopicSettings("topic_2", 4, TimeSpan.FromDays(7)),
            ]);
        
        var tpq = new TpqServices(tpqConfig);
        tpq.Initializer.Initialize();
        
        Console.WriteLine("Coming soon...");
    }
}