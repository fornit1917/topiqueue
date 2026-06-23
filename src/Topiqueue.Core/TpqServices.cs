using Topiqueue.Core.Configuration;
using Microsoft.Extensions.Logging;
using Topiqueue.Core.BackgroundService;
using Topiqueue.Core.BackgroundService.RotateSegments;
using Topiqueue.Core.Helpers;
using Topiqueue.Core.Initializer;

namespace Topiqueue.Core;

public class TpqServices
{
    public ITpqInitializer Initializer { get; }
    public ITpqBackgroundService BackgroundService { get; }

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
            config.BackgroundServiceSettings);
        
        var rotateSegmentsService = new RotateSegmentsService(
            topicsDao, 
            TimerService.Instance,
            config.LoggerFactory.CreateLogger<RotateSegmentsService>(),
            config.Topics,
            config.BackgroundServiceSettings);

        BackgroundService = new TpqBackgroundService(rotateSegmentsService);
    }
}