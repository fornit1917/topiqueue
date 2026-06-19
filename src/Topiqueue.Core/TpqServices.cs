using Topiqueue.Core.Configuration;
using Microsoft.Extensions.Logging;
using Topiqueue.Core.Initializer;

namespace Topiqueue.Core;

public class TpqServices
{
    public ITpqInitializer Initializer { get; }

    public TpqServices(TpqConfig config)
    {
        var daoFactory = config.DaoFactory;
        var dbMigrator = daoFactory.CreateMigrator();
        var topicsDao = daoFactory.CreateTopicsDao();
        
        Initializer = new TpqInitializer(
            dbMigrator,
            topicsDao,
            config.LoggerFactory.CreateLogger<TpqInitializer>(),
            config.Topics,
            config.CheckSegmentsSettings);
    }
}