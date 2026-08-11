using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Topiqueue.Core.BackgroundService.Consumers.Commands;
using Topiqueue.Core.BackgroundService.Consumers.Commands.Dao;
using Topiqueue.Core.BackgroundService.Consumers.Commands.Handlers;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces.CommandBus;
using Topiqueue.Core.BackgroundService.Consumers.Models;
using Topiqueue.Core.Dao.Models;
using Topiqueue.Core.Helpers;
using Topiqueue.Core.Messages.Models;

namespace Topiqueue.Core.BackgroundService.Consumers.Services;

internal class ConsumersDispatcherService : IConsumersDispatcherService
{
    private readonly Channel<ConsumersCommand> _channel;
    private readonly IPartitionsRegistry _partitions;
    private readonly IDaoCommandBus _daoCommandBus;
    private readonly IHandlersCommandBus _handlersCommandBus;
    private readonly ITimerService _timer;
    private readonly ILogger<ConsumersDispatcherService> _logger;
    private readonly string _serverId;
    
    private readonly Stack<List<TpqMessageModel>> _messagesBufferPool = new();
    
    public ConsumersDispatcherService(
        Channel<ConsumersCommand> channel,
        IPartitionsRegistry partitions,
        ITimerService timer,
        IDaoCommandBus daoCommandBus,
        IHandlersCommandBus handlersCommandBus,
        ILogger<ConsumersDispatcherService> logger,
        string serverId)
    {
        _partitions = partitions;
        _logger = logger;
        _serverId = serverId;
        _daoCommandBus = daoCommandBus;
        _handlersCommandBus = handlersCommandBus;
        _timer = timer;
        _channel = channel;
    }

    public void Run(CancellationToken cancellationToken)
    {
        _ = Task.Run(async () => await HandleCommandsProcess(cancellationToken), cancellationToken);
    }

    private async ValueTask HandleCommandsProcess(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var channelOpen = await _channel.Reader.WaitToReadAsync(cancellationToken);
                if (!channelOpen)
                {
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!cancellationToken.IsCancellationRequested && _channel.Reader.TryRead(out var command))
            {
                await HandleCommand(command);
            }
        }
    }

    private ValueTask HandleCommand(ConsumersCommand command)
    {
        return command.Type switch
        {
            ConsumersCommandType.CapturePartitions => HandleCaptureCommand(command.AsCapturePartitions()),
            ConsumersCommandType.SetPartitionCaptured => HandleSetCapturedCommand(command.AsSetCapturedPartition()),
            
            ConsumersCommandType.ReleasePartitions => HandleReleaseCommand(command.AsReleasePartitions()),
            ConsumersCommandType.SetPartitionReleased => HandleSetReleasedCommand(command.AsSetReleasedPartitions()),
            
            ConsumersCommandType.LoadMessages => HandleLoadMessagesCommand(command.AsLoadMessages()),
            ConsumersCommandType.SetLoadedMessages => HandleSetLoadedMessagesCommand(command.AsSetLoadedMessages()),
            
            ConsumersCommandType.Commit => HandleCommitCommand(command.AsCommit()),
            ConsumersCommandType.SetCommitted => HandleSetCommittedCommand(command.AsSetCommitted()),
            
            _ => ValueTask.CompletedTask
        };
    }

    private ValueTask HandleCaptureCommand(CapturePartitionsCommand command)
    {
        var daoCommand = new CapturePartitionsDaoCommand
        {
            Consumer = command.Consumer,
            PartitionsCount = command.PartitionsCount
        };
        return _daoCommandBus.Send(daoCommand);
    }
    
    private ValueTask HandleSetCapturedCommand(SetCapturedPartitionCommand command)
    {
        var partition = _partitions.Get(command.Consumer, command.CapturedPartition.PartitionNum);
        var offset = new PartitionOffset
        {
            TxId = command.CapturedPartition.LastProcessedTxId,
            SeqId = command.CapturedPartition.LastProcessedSeqId,
            CreatedAt = command.CapturedPartition.LastProcessedCreatedAt
        };
        partition.SetCaptured(offset);
        
        _logger.LogInformation("Partition {PartitionNum} of topic {TopicName} has been captured by server {ServerId}",
            command.CapturedPartition.PartitionNum, command.Consumer.TopicName, _serverId);

        var readCommand = new ConsumersCommand(new LoadMessagesCommand
        {
            Consumer = command.Consumer,
            PartitionNum = command.CapturedPartition.PartitionNum,
            Offset = new PartitionOffset
            {
                TxId = command.CapturedPartition.LastProcessedTxId,
                SeqId = command.CapturedPartition.LastProcessedSeqId,
                CreatedAt = command.CapturedPartition.LastProcessedCreatedAt
            }
        });
        return _channel.Writer.WriteAsync(readCommand);
    }

    private ValueTask HandleLoadMessagesCommand(LoadMessagesCommand command)
    {
        var partition = _partitions.Get(command.Consumer, command.PartitionNum);
        if (!partition.Captured || partition.ReadInProgress || partition.ReadOnPause)
        {
            return ValueTask.CompletedTask;
        }
        
        partition.ReadInProgress = true;

        var messagesBuffer = GetMessagesBuffer();
        var daoCommand = new LoadMessagesDaoCommand
        {
            Consumer = command.Consumer,
            PartitionNum = command.PartitionNum,
            Offset = command.Offset,
            MessagesBuffer = messagesBuffer
        };
        return _daoCommandBus.Send(daoCommand);
    }

    private async ValueTask HandleSetLoadedMessagesCommand(SetLoadedMessagesCommand command)
    {
        ArgumentNullException.ThrowIfNull(command.MessagesBuffer, nameof(command.MessagesBuffer));
        
        var partition = _partitions.Get(command.Consumer, command.PartitionNum);
        if (!partition.Captured)
        {
            ReturnToPool(command.MessagesBuffer);
            return;
        }
        
        partition.ReadInProgress = false;
        
        // _logger.LogInformation($"Loaded {command.MessagesBuffer.Count} from {command.Consumer.TopicName}.{command.PartitionNum}");

        if (command.MessagesBuffer.Count == 0)
        {
            ReturnToPool(command.MessagesBuffer);

            if (!partition.ReadOnPause)
            {
                partition.ReadOnPause = true;

                var retryReadCommand = new ConsumersCommand(new LoadMessagesCommand
                {
                    Consumer = command.Consumer,
                    PartitionNum = command.PartitionNum,
                    Offset = command.UsedOffset
                });
            
                _ = _timer.RunWithDelay(() =>
                {
                    partition.ReadOnPause = false;
                    return _channel.Writer.WriteAsync(retryReadCommand);
                }, command.Consumer.EmptyTopicPause);                
            }
        }
        else
        {
            partition.AddMessagesToCache(command.MessagesBuffer);
            
            ReturnToPool(command.MessagesBuffer);
            
            await SendBatchToHandlerIfNeed(partition);
            await ReadNextToCacheIfNeed(partition);
        }
    }

    private ValueTask HandleCommitCommand(CommitCommand command)
    {
        ReturnToPool(command.HandledMessagesBuffer);
        
        var partition = _partitions.Get(command.Consumer, command.PartitionNum);
        if (!partition.Captured)
        {
            return ValueTask.CompletedTask;
        }

        var daoCommand = new CommitDaoCommand
        {
            Consumer = command.Consumer,
            PartitionNum = command.PartitionNum,
            Offset = command.Offset
        };
        
        return _daoCommandBus.Send(daoCommand);
    }

    private async ValueTask HandleSetCommittedCommand(SetCommittedCommand command)
    {
        var partition = _partitions.Get(command.Consumer, command.PartitionNum);

        if (!command.IsStillCaptured)
        {
            partition.SetReleased();
            return;
        }
        
        if (!partition.Captured)
        {
            return;
        }
        
        partition.CommitedOffset = command.Offset;
        partition.HandleInProgress = false;
        
        // _logger.LogInformation($"Committed {command.Offset.TxId}.{command.Offset.SeqId} for {command.Consumer.TopicName}.{command.PartitionNum}");

        if (partition.ReleaseRequested)
        {
            var releaseCommand = new ReleasePartitionsDaoCommand
            {
                Consumer = command.Consumer,
                PartitionsNums = [partition.PartitionNum]
            };
            await _daoCommandBus.Send(releaseCommand);
            return;
        }

        if (partition.MessagesInCacheCount > 0)
        {
            await SendBatchToHandlerIfNeed(partition);
        }
        
        await ReadNextToCacheIfNeed(partition);
    }
    
    private ValueTask HandleReleaseCommand(ReleasePartitionsCommand command)
    {
        var partitionNums = new List<int>(capacity: command.PartitionsCount);
        foreach (var partition in _partitions.GetCaptured())
        {
            partition.ReleaseRequested = true;
            if (!partition.HandleInProgress)
            {
                partitionNums.Add(partition.PartitionNum);
            }

            if (partitionNums.Count >= command.PartitionsCount)
            {
                break;
            }
        }

        if (partitionNums.Count > 0)
        {
            var daoCommand = new ReleasePartitionsDaoCommand
            {
                Consumer = command.Consumer,
                PartitionsNums = partitionNums
            };
            return _daoCommandBus.Send(daoCommand);
        }
        
        return ValueTask.CompletedTask;
    }
    
    private ValueTask HandleSetReleasedCommand(SetReleasedPartitionCommand command)
    {
        var partition = _partitions.Get(command.Consumer, command.PartitionNum);
        partition.SetReleased();
        
        _logger.LogInformation("Partition {PartitionNum} of topic {TopicName} has been released by server {ServerId}",
            command.PartitionNum, command.Consumer.TopicName, _serverId);
        
        return ValueTask.CompletedTask;
    }

    private List<TpqMessageModel> GetMessagesBuffer()
    {
        if (!_messagesBufferPool.TryPop(out var messagesBuffer))
        {
            messagesBuffer = new List<TpqMessageModel>();
        }

        return messagesBuffer;
    }

    private void ReturnToPool(List<TpqMessageModel> messagesBuffer)
    {
        messagesBuffer.Clear();
        _messagesBufferPool.Push(messagesBuffer);
    }

    private ValueTask SendBatchToHandlerIfNeed(PartitionState partition)
    {
        if (partition.HandleInProgress || partition.MessagesInCacheCount == 0)
            return ValueTask.CompletedTask;
        
        partition.HandleInProgress = true;
        
        var messagesToHandle = GetMessagesBuffer();
        partition.DequeBatchForHandleFromCache(messagesToHandle);

        var handleMessagedCommand = new HandleMessagesCommand
        {
            Consumer = partition.Consumer,
            PartitionNum = partition.PartitionNum,
            MessagesBuffer = messagesToHandle
        };
        
        return _handlersCommandBus.Send(handleMessagedCommand);
    }

    private ValueTask ReadNextToCacheIfNeed(PartitionState partition)
    {
        if (partition.ReadInProgress
            || partition.ReadOnPause
            || partition.MessagesInCacheCount >= partition.Consumer.ReaderBatchSize
            || !partition.LastReadOffset.HasValue)
        {
            return ValueTask.CompletedTask;
        }
        
        var lastReadOffset = partition.LastReadOffset.Value;
        
        var readNextCommand = new ConsumersCommand(new LoadMessagesCommand
        {
            Consumer = partition.Consumer,
            PartitionNum = partition.PartitionNum,
            Offset = lastReadOffset
        });
        
        return _channel.Writer.WriteAsync(readNextCommand);
    }
}