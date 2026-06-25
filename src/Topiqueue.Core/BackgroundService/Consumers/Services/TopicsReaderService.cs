using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces;
using Topiqueue.Core.BackgroundService.Consumers.Models;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Dao;

namespace Topiqueue.Core.BackgroundService.Consumers.Services;

internal class TopicsReaderService : ITopicsReaderService
{
    private readonly ChannelReader<TopicsReaderCommand> _channelReader;
    private readonly ITopicsReaderCommandBus _topicsReaderCommandBus;
    private readonly IMessagesHandlerCommandBus _messagesHandlerCommandBus;
    private readonly ITpqConsumerDao _consumerDao;
    private readonly ILogger<TopicsReaderService> _logger;
    private readonly IReadOnlyList<TpqConsumerSettings> _consumers;
    private readonly TpqBackgroundServiceSettings _backgroundServiceSettings;
    private readonly string _serverId;

    public TopicsReaderService(
        ChannelReader<TopicsReaderCommand> channelReader,
        ITopicsReaderCommandBus topicsReaderCommandBus,
        IMessagesHandlerCommandBus messagesHandlerCommandBus,
        ITpqConsumerDao consumerDao,
        ILogger<TopicsReaderService> logger,
        IReadOnlyList<TpqConsumerSettings> consumers,
        TpqBackgroundServiceSettings backgroundServiceSettings,
        string serverId)
    {
        _channelReader = channelReader;
        _topicsReaderCommandBus = topicsReaderCommandBus;
        _messagesHandlerCommandBus = messagesHandlerCommandBus;
        _consumerDao = consumerDao;
        _logger = logger;
        _consumers = consumers;
        _backgroundServiceSettings = backgroundServiceSettings;
        _serverId = serverId;
    }

    public void Run(CancellationToken cancellationToken)
    {
        for (int i = 0; i < _backgroundServiceSettings.TopicsReaderWorkers; i++)
        {
            _ = Task.Run(async () => await ReadChannel(cancellationToken), cancellationToken);
        }

        foreach (var consumer in _consumers)
        {
            _topicsReaderCommandBus.SendTryCapturePartitionsCommand(consumer, consumer.TryCapturePartitionsOnStart);
        }
    }

    private async Task ReadChannel(CancellationToken cancellationToken)
    {
        var partitionsNumbers = new List<int>();
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var channelOpen = await _channelReader.WaitToReadAsync(cancellationToken);
                if (!channelOpen)
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }

            while (_channelReader.TryRead(out var command))
            {
                if (command.Type == TopicsReaderCommandType.TryCapturePartitions)
                {
                    await HandleTryCapturePartitions(command, partitionsNumbers);
                }
            }
        }
    }

    private async Task HandleTryCapturePartitions(TopicsReaderCommand command, List<int> capturedPartitionNumbers)
    {
        await _consumerDao.TryCapturePartitionsAsync(_serverId, command.Consumer, command.PartitionsCount,
            capturedPartitionNumbers);
        
        foreach (var partitionNum in capturedPartitionNumbers)
        {
            _logger.LogInformation("Partition {PartitionNum} of topic {TopicName} in group {GroupId} has been captured by server {ServerId}",
                partitionNum, command.Consumer.TopicName, command.Consumer.ConsumerGroupId, _serverId);
        }
    }
}