using System;
using System.Collections.Frozen;
using System.Threading.Channels;
using Topiqueue.Core.Configuration;
using Microsoft.Extensions.Logging;
using Topiqueue.Core.BackgroundService;
using Topiqueue.Core.BackgroundService.Consumers.Commands;
using Topiqueue.Core.BackgroundService.Consumers.Commands.Dao;
using Topiqueue.Core.BackgroundService.Consumers.Commands.Handlers;
using Topiqueue.Core.BackgroundService.Consumers.Models;
using Topiqueue.Core.BackgroundService.Consumers.Services;
using Topiqueue.Core.BackgroundService.Consumers.Services.CommandBus;
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
    public string ServerId { get; }

    public TpqServices(TpqConfig config)
    {
        var topicsRegistry = new TopicsRegistry(config.Topics);
        ServerId = $"{Environment.MachineName}_{Guid.NewGuid()}";
        
        // todo: add validation for consumers
        
        Initializer = new TpqInitializer(
            config.Dao,
            config.LoggerFactory.CreateLogger<TpqInitializer>(),
            topicsRegistry,
            config.Consumers,
            config.BackgroundServiceSettings,
            ServerId);
        
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
            ServerId);
        
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
        var consumersDaoCommandBus = new DaoCommandBus(consumersDaoServiceChannel.Writer);

        var handlersServiceChannelOpts = new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = false,
            SingleWriter = true,
        };
        var handlersServiceChannel = Channel.CreateUnbounded<HandleMessagesCommand>(handlersServiceChannelOpts);
        var handlersCommandBus = new HandlersCommandBus(handlersServiceChannel.Writer);
        
        var consumersDispatcherChannelOpts = new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false
        };
        var consumersDispatcherChannel = Channel.CreateUnbounded<ConsumersCommand>(consumersDispatcherChannelOpts);
        var consumersDaoResultCommandBus = new DaoResultCommandBus(consumersDispatcherChannel.Writer);
        var handlersResultCommandBus = new HandlersResultCommandBus(consumersDispatcherChannel.Writer);
        var partitionsCommandBus = new PartitionsCommandBus(consumersDispatcherChannel.Writer);
        
        var consumersContext = new ConsumersContext
        {
            Settings = config.BackgroundServiceSettings,
            Consumers = config.Consumers,
            Topics = topicsRegistry,
            ServerId = ServerId,
        };
        
        var consumersDaoService = new ConsumersDaoService(
            consumersDaoServiceChannel,
            config.Dao.ConsumerDao,
            TimerService.Instance,
            consumersDaoResultCommandBus,
            consumersContext,
            config.LoggerFactory.CreateLogger<ConsumersDaoService>());

        var handlerExecutors = config
            .ExecutorsByMessageType
            .ToFrozenDictionary(x => x.Key, x => x.Value);
        var handlersRegistry = new HandlersRegistry(handlerExecutors);
        var handlersService = new HandlersService(
            handlersServiceChannel,
            config.ServiceContainerScopeFactory,
            handlersRegistry,
            handlersResultCommandBus,
            TimerService.Instance,
            consumersContext,
            config.LoggerFactory.CreateLogger<HandlersService>());
        
        var partitionsRegistry = new PartitionsRegistry(topicsRegistry, config.Consumers);
        var consumersDispatcherService = new ConsumersDispatcherService(
            consumersDispatcherChannel,
            partitionsRegistry,
            TimerService.Instance,
            consumersDaoCommandBus,
            handlersCommandBus,
            config.LoggerFactory.CreateLogger<ConsumersDispatcherService>(),
            ServerId);
        
        var partitionsBalancerService = new PartitionsBalancerService(
            config.Dao.ServersDao,
            config.Dao.ConsumerDao,
            TimerService.Instance,
            partitionsCommandBus,
            config.LoggerFactory.CreateLogger<PartitionsBalancerService>(),
            consumersContext);

        BackgroundService = new TpqBackgroundService(
            rotateSegmentsService,
            heartbeatService,
            partitionsBalancerService,
            consumersDispatcherService,
            consumersDaoService,
            handlersService);        
    }
}