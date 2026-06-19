using Npgsql;
using Topiqueue.Core.Configuration;
using Topiqueue.Postgres.Dao;

namespace Topiqueue.Postgres.Configuration;

public static class TpqConfigExtensions
{
    public static TpqConfig UsePostgresql(this TpqConfig config, 
        NpgsqlDataSource dataSource,
        TpqPostgresSettings? settings = null)
    {
        settings ??= new TpqPostgresSettings();
        config.UseDaoFactory(commonInfra => new PgsqlDaoFactory(dataSource, settings, commonInfra.LoggerFactory));
        return config;
    }
}