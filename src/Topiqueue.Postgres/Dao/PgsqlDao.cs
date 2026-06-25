using Microsoft.Extensions.Logging;
using Npgsql;
using Topiqueue.Core.Dao;
using Topiqueue.Postgres.Configuration;

namespace Topiqueue.Postgres.Dao;

internal class PgsqlDao : ITpqDao
{
    public ITpqDbMigrator Migrator { get; }
    public ITpqTopicsDao TopicsDao { get; }
    public ITpqProducerDao ProducerDao { get; }
    public ITpqServersDao ServersDao { get; }
    public ITpqConsumerDao ConsumerDao { get; }

    public PgsqlDao(NpgsqlDataSource dataSource, TpqPostgresSettings settings, ILoggerFactory loggerFactory)
    {
        Migrator = new PgsqlDbMigrator(dataSource, settings, loggerFactory.CreateLogger<PgsqlDbMigrator>());
        TopicsDao = new PgsqlTopicsDao(dataSource, settings);
        ProducerDao = new PgsqlProducerDao(dataSource, settings);
        ServersDao = new PgsqlServersDao(dataSource, settings);
        ConsumerDao = new PgsqlConsumerDao(dataSource, settings);
    }
}