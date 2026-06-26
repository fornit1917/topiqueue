using System;
using System.Threading.Channels;
using Topiqueue.Core.Configuration;
using Microsoft.Extensions.Logging;
using Topiqueue.Core.BackgroundService;
using Topiqueue.Core.BackgroundService.Consumers.Models;
using Topiqueue.Core.BackgroundService.Consumers.Models.Commands;
using Topiqueue.Core.BackgroundService.Consumers.Services;
using Topiqueue.Core.BackgroundService.Heartbeat;
using Topiqueue.Core.BackgroundService.SegmentsRotation;
using Topiqueue.Core.Helpers;
using Topiqueue.Core.Initializer;
using Topiqueue.Core.Messages.Interfaces;
using Topiqueue.Core.Messages.Services;
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
        var topicsRegistry = new TopicsRegistry(config.Topics);
        var serverId = $"{Environment.MachineName}_{Guid.NewGuid()}";
        
        // todo: add validation for consumers
        
        Initializer = new TpqInitializer(
            config.Dao,
            config.LoggerFactory.CreateLogger<TpqInitializer>(),
            topicsRegistry,
            config.Consumers,
            config.BackgroundServiceSettings,
            serverId);
        
        var rotateSegmentsService = new SegmentsRotationService(
            config.Dao.TopicsDao, 
            TimerService.Instance,
            config.LoggerFactory.CreateLogger<SegmentsRotationService>(),
            config.Topics,
            config.BackgroundServiceSettings);
        
        var heartbeatService = new HeartbeatService(
            config.Dao.ServersDao,
            TimerService.Instance,
            config.LoggerFactory.CreateLogger<HeartbeatService>(),
            config.Consumers,
            config.BackgroundServiceSettings,
            serverId);
        
        MessageFactory = new MessageFactory(
            topicsRegistry,
            config.Serializer,
            PartitionNumCalculator.Instance);

        Producer = new TpqProducer(MessageFactory, config.Dao.ProducerDao);
        
        var messagesHandlerChannelOptions = new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = config.BackgroundServiceSettings.MessagesHandlerWorkers == 1,
            AllowSynchronousContinuations = false,
        };
        var messagesHandlerChannel = Channel.CreateUnbounded<MessagesHandlerCommand>(messagesHandlerChannelOptions);
        
        var topicsReaderChannelOptions = new UnboundedChannelOptions()
        {
            SingleWriter = false,
            SingleReader = config.BackgroundServiceSettings.TopicsReaderWorkers == 1,
            AllowSynchronousContinuations = false
        };
        var topicsReaderChannel = Channel.CreateUnbounded<TopicsReaderCommand>(topicsReaderChannelOptions);
        
        var commandBus = new ConsumersCommandBus(topicsReaderChannel.Writer);
        
        var consumersContext = new ConsumersContext(topicsRegistry, config.Consumers, commandBus);
        
        var topicsReaderService = new TopicsReaderService(
            topicsReaderChannel.Reader,
            consumersContext,
            config.Dao.ConsumerDao,
            TimerService.Instance,
            config.LoggerFactory.CreateLogger<TopicsReaderService>(),
            config.BackgroundServiceSettings,
            serverId);
        
        var partitionsBalancerService = new PartitionsBalancerService(
            consumersContext,
            config.Dao.ServersDao,
            TimerService.Instance,
            config.LoggerFactory.CreateLogger<PartitionsBalancerService>(),
            config.BackgroundServiceSettings);

        BackgroundService = new TpqBackgroundService(
            rotateSegmentsService,
            heartbeatService,
            partitionsBalancerService,
            topicsReaderService);        
    }
}