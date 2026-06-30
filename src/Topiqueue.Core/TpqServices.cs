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

        var consumersDaoServiceChannelOpts = new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = false,
            SingleWriter = true,
        };
        var consumersDaoServiceChannel = Channel.CreateUnbounded<DaoCommand>(consumersDaoServiceChannelOpts);
        var consumersDaoServiceChannelWriter = new CommandWriter<DaoCommand>(consumersDaoServiceChannel.Writer);
        
        var consumersDispatcherChannelOpts = new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        };
        var consumersDispatcherChannel = Channel.CreateUnbounded<ConsumersCommand>(consumersDispatcherChannelOpts);
        var consumersDispatcherChannelWriter = new CommandWriter<ConsumersCommand>(consumersDispatcherChannel.Writer);
        
        
        var consumersContext = new ConsumersContext
        {
            Settings = config.BackgroundServiceSettings,
            Consumers = config.Consumers,
            Topics = topicsRegistry,
            ServerId = serverId,
            CommandsWriter = consumersDispatcherChannelWriter
        };
        
        var consumersDaoService = new ConsumersDaoService(
            consumersDaoServiceChannel,
            config.Dao.ConsumerDao,
            TimerService.Instance,
            consumersContext,
            config.LoggerFactory.CreateLogger<ConsumersDaoService>());
        
        var partitionsRegistry = new PartitionsRegistry(topicsRegistry, config.Consumers);
        var consumersDispatcherService = new ConsumersDispatcherService(
            consumersDispatcherChannel,
            partitionsRegistry,
            consumersDaoServiceChannelWriter,
            config.LoggerFactory.CreateLogger<ConsumersDispatcherService>(),
            serverId);
        
        var partitionsBalancerService = new PartitionsBalancerService(
            config.Dao.ServersDao,
            config.Dao.ConsumerDao,
            TimerService.Instance,
            config.LoggerFactory.CreateLogger<PartitionsBalancerService>(),
            consumersContext);

        BackgroundService = new TpqBackgroundService(
            rotateSegmentsService,
            heartbeatService,
            partitionsBalancerService,
            consumersDispatcherService,
            consumersDaoService);        
    }
}