using Topiqueue.Core.Configuration;
using Microsoft.Extensions.Logging;
using Topiqueue.Core.BackgroundService;
using Topiqueue.Core.BackgroundService.RotateSegments;
using Topiqueue.Core.Helpers;
using Topiqueue.Core.Initializer;
using Topiqueue.Core.Messages;
using Topiqueue.Core.Producer;

namespace Topiqueue.Core;

public class TpqServices
{
    public ITpqInitializer Initializer { get; }
    public ITpqBackgroundService BackgroundService { get; }
    public ITpqMessageFactory MessageFactory { get; }
    public ITpqProducer Producer { get; }

    public TpqServices(TpqConfig config)
    {
        var daoFactory = config.DaoFactory;
        var dbMigrator = daoFactory.CreateMigrator();
        var topicsDao = daoFactory.CreateTopicsDao();
        var messagesDao = daoFactory.CreateMessagesDao();
        var topicsRegistry = new TopicsRegistry(config.Topics);
        
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
        
        MessageFactory = new MessageFactory(
            topicsRegistry,
            config.Serializer,
            PartitionNumCalculator.Instance);

        Producer = new TpqProducer(MessageFactory, messagesDao);
    }
}