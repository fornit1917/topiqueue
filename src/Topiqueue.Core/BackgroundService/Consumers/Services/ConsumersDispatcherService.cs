using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces;
using Topiqueue.Core.BackgroundService.Consumers.Models.Commands;

namespace Topiqueue.Core.BackgroundService.Consumers.Services;

internal class ConsumersDispatcherService : IConsumersDispatcherService
{
    private readonly Channel<ConsumersCommand> _channel;
    private readonly IPartitionsRegistry _partitions;
    private readonly ICommandWriter<DaoCommand> _daoCommandsWriter;
    private readonly ILogger<ConsumersDispatcherService> _logger;
    private readonly string _serverId;
    
    public ConsumersDispatcherService(
        Channel<ConsumersCommand> channel,
        IPartitionsRegistry partitions,
        ICommandWriter<DaoCommand> daoCommandsWriter,
        ILogger<ConsumersDispatcherService> logger,
        string serverId)
    {
        _partitions = partitions;
        _daoCommandsWriter = daoCommandsWriter;
        _logger = logger;
        _serverId = serverId;
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
            ConsumerCommandType.CapturePartitions => HandleCaptureCommand(command),
            ConsumerCommandType.SetPartitionCaptured => HandleSetCapturedCommand(command),
            ConsumerCommandType.ReleasePartitions => HandleReleaseCommand(command),
            ConsumerCommandType.SetPartitionReleased => HandleSetReleasedCommand(command),
            _ => ValueTask.CompletedTask
        };
    }

    private ValueTask HandleCaptureCommand(ConsumersCommand command)
    {
        var daoCommand = DaoCommand.CapturePartitions(command.Consumer, command.PartitionsCount);
        return _daoCommandsWriter.Write(daoCommand);
    }
    
    private ValueTask HandleSetCapturedCommand(ConsumersCommand command)
    {
        if (command.CapturedPartition == null)
        {
            _logger.LogError("Invalid SetCapturedCommand. CapturedPartition is null");
            return ValueTask.CompletedTask;
        }
        
        var partition = _partitions.Get(command.Consumer, command.CapturedPartition.PartitionNum);
        partition.Captured = true;
        partition.ReleaseRequested = false;
        
        _logger.LogInformation("Partition {PartitionNum} of topic {TopicName} has been captured by server {ServerId}",
            command.CapturedPartition.PartitionNum, command.Consumer.TopicName, _serverId);
        
        // todo: send ReadMessages command
        return ValueTask.CompletedTask;
    }
    
    private ValueTask HandleReleaseCommand(ConsumersCommand command)
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
            var daoCommand = DaoCommand.ReleasePartitions(command.Consumer, partitionNums);
            return _daoCommandsWriter.Write(daoCommand);
        }
        
        return ValueTask.CompletedTask;
    }
    
    private ValueTask HandleSetReleasedCommand(ConsumersCommand command)
    {
        var partition = _partitions.Get(command.Consumer, command.PartitionNum);
        partition.Captured = false;
        
        _logger.LogInformation("Partition {PartitionNum} of topic {TopicName} has been released by server {ServerId}",
            command.PartitionNum, command.Consumer.TopicName, _serverId);
        
        return ValueTask.CompletedTask;
    }
}