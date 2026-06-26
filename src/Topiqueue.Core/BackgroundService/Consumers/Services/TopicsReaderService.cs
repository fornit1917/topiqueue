using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces;
using Topiqueue.Core.BackgroundService.Consumers.Models;
using Topiqueue.Core.BackgroundService.Consumers.Models.Commands;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Dao;
using Topiqueue.Core.Helpers;

namespace Topiqueue.Core.BackgroundService.Consumers.Services;

internal class TopicsReaderService : ITopicsReaderService
{
    private readonly ChannelReader<TopicsReaderCommand> _channelReader;
    private readonly IConsumersContext _context;
    private readonly ITpqConsumerDao _consumerDao;
    private readonly ITimerService _timerService;
    private readonly ILogger<TopicsReaderService> _logger;
    private readonly TpqBackgroundServiceSettings _settings;
    private readonly string _serverId;

    public TopicsReaderService(
        ChannelReader<TopicsReaderCommand> channelReader,
        IConsumersContext context,
        ITpqConsumerDao consumerDao,
        ITimerService timerService,
        ILogger<TopicsReaderService> logger,
        TpqBackgroundServiceSettings settings,
        string serverId)
    {
        _channelReader = channelReader;
        _context = context;
        _consumerDao = consumerDao;
        _timerService = timerService;
        _logger = logger;
        _settings = settings;
        _serverId = serverId;
    }

    public void Run(CancellationToken cancellationToken)
    {
        for (int i = 0; i < _settings.TopicsReaderWorkers; i++)
        {
            _ = Task.Run(async () => await ReadChannel(cancellationToken), cancellationToken);
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

            while (!cancellationToken.IsCancellationRequested && _channelReader.TryRead(out var command))
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        switch (command.Type)
                        {
                            case TopicsReaderCommandType.TryCapturePartitions:
                                await HandleTryCapturePartitions(command, partitionsNumbers);
                                break;
                            case TopicsReaderCommandType.ReleasePartitions:
                                await HandleTryReleasePartitions(command, partitionsNumbers);
                                break;
                        }

                        break;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error while handle TopicsReaderCommand with type {CommandType}. The next attempt will be in {DbErrorPause}",
                            command.Type.ToString(), _settings.DbErrorPause);
                        
                        await _timerService.TryDelay(_settings.DbErrorPause, cancellationToken);
                    }
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
            if (_context.TrySetCaptured(command.Consumer, partitionNum))
            {
                _logger.LogInformation("Partition {PartitionNum} of topic {TopicName} in group {GroupId} has been captured by server {ServerId}",
                    partitionNum, command.Consumer.TopicName, command.Consumer.ConsumerGroupId, _serverId);                
            }
        }
    }

    private async Task HandleTryReleasePartitions(TopicsReaderCommand command, List<int> releasedPartitionNumbers)
    {
        await _consumerDao.TryReleasePartitionsAsync(_serverId, command.Consumer, command.PartitionsCount,
            releasedPartitionNumbers);
        
        foreach (var partitionNum in releasedPartitionNumbers)
        {
            if (_context.TrySetReleased(command.Consumer, partitionNum))
            {
                _logger.LogInformation("Partition {PartitionNum} of topic {TopicName} in group {GroupId} has been released by server {ServerId}",
                    partitionNum, command.Consumer.TopicName, command.Consumer.ConsumerGroupId, _serverId);                
            }
        }    
    }
}