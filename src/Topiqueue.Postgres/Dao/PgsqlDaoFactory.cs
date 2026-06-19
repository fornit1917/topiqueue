using Microsoft.Extensions.Logging;
using Npgsql;
using Topiqueue.Core.Dao;
using Topiqueue.Postgres.Configuration;

namespace Topiqueue.Postgres.Dao;

internal class PgsqlDaoFactory : ITpqDaoFactory
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly TpqPostgresSettings _settings;
    private readonly ILoggerFactory _loggerFactory;

    public PgsqlDaoFactory(NpgsqlDataSource dataSource, TpqPostgresSettings settings, ILoggerFactory loggerFactory)
    {
        _dataSource = dataSource;
        _settings = settings;
        _loggerFactory = loggerFactory;
    }

    public ITpqDbMigrator CreateMigrator()
    {
        return new PgsqlDbMigrator(_dataSource, _settings, _loggerFactory.CreateLogger<PgsqlDbMigrator>());
    }

    public ITpqTopicsDao CreateTopicsDao()
    {
        return new PgsqlTopicsDao(_dataSource, _settings);
    }
}