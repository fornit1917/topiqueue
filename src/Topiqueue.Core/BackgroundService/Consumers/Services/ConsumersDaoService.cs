using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Topiqueue.Core.BackgroundService.Consumers.Commands;
using Topiqueue.Core.BackgroundService.Consumers.Commands.Dao;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces.CommandBus;
using Topiqueue.Core.Dao;
using Topiqueue.Core.Dao.Models;
using Topiqueue.Core.Helpers;

namespace Topiqueue.Core.BackgroundService.Consumers.Services;

internal class ConsumersDaoService : IConsumersDaoService
{
    private readonly Channel<DaoCommand> _channel;
    private readonly ITpqConsumerDao _consumerDao;
    private readonly ITimerService _timerService;
    private readonly IDaoResultCommandBus _resultCommandBus;
    private readonly IConsumersContext _context;
    private readonly ILogger<ConsumersDaoService> _logger;

    public ConsumersDaoService(
        Channel<DaoCommand> channel,
        ITpqConsumerDao consumerDao,
        ITimerService timerService,
        IDaoResultCommandBus resultCommandBus,
        IConsumersContext context,
        ILogger<ConsumersDaoService> logger)
    {
        _channel = channel;
        _consumerDao = consumerDao;
        _timerService = timerService;
        _resultCommandBus = resultCommandBus;
        _context = context;
        _logger = logger;
    }

    public void Run(CancellationToken cancellationToken)
    {
        for (int i = 0; i < _context.Settings.DbQueryExecutorWorkers; i++)
        {
            _ = Task.Run(async () => await HandleCommandsProcess(cancellationToken), cancellationToken);
        }
    }

    private async Task HandleCommandsProcess(CancellationToken cancellationToken)
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

                while (!cancellationToken.IsCancellationRequested && _channel.Reader.TryRead(out var command))
                {
                    await HandleCommand(command, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task HandleCommand(DaoCommand command, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var task = command.Type switch
                {
                    DaoCommandType.CapturePartitions => HandleCapturePartitions(command.AsCapturePartitions(), cancellationToken),
                    DaoCommandType.ReleasePartitions => HandleReleasePartitions(command.AsReleasePartitions(), cancellationToken),
                    DaoCommandType.LoadMessages => HandleLoadMessages(command.AsLoadMessages(), cancellationToken),
                    DaoCommandType.CommitOffset => HandleCommitOffset(command.AsCommitOffset(), cancellationToken),
                    _ => Task.CompletedTask
                };
                
                await task;

                return;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error in DaoCommandsExecutorService while processing command {CommandType}. The next attempt will be in {DbErrorPause}",
                    command.Type.ToString(), _context.Settings.DbErrorPause);
                
                await _timerService.TryDelay(_context.Settings.DbErrorPause, cancellationToken);
            }
        }
    }

    private async Task HandleCapturePartitions(CapturePartitionsDaoCommand command, CancellationToken cancellationToken)
    {
        var capturedPartitions = await _consumerDao.CapturePartitionsAsync(_context.ServerId, command.Consumer, command.PartitionsCount);
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        foreach (var partition in capturedPartitions)
        {
            var setCapturedCommand = new SetCapturedPartitionCommand
            {
                Consumer = command.Consumer,
                CapturedPartition = partition
            };
            await _resultCommandBus.Send(setCapturedCommand);
        }
    }

    private async Task HandleReleasePartitions(ReleasePartitionsDaoCommand command, CancellationToken cancellationToken)
    {
        await _consumerDao.ReleasePartitionsAsync(_context.ServerId, command.Consumer, command.PartitionsNums);
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        
        foreach (var releasedPartition in command.PartitionsNums)
        {
            var setReleasedCommand = new SetReleasedPartitionCommand
            {
                Consumer = command.Consumer,
                PartitionNum = releasedPartition
            };
            await _resultCommandBus.Send(setReleasedCommand);
        }
    }

    private async Task HandleLoadMessages(LoadMessagesDaoCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command.MessagesBuffer, nameof(command.MessagesBuffer));
        
        var readMessagesRequest = new ReadMessagesRequest
        {
            TopicName = command.Consumer.TopicName,
            PartitionNum = command.PartitionNum,
            Offset = command.Offset,
            Limit = command.Consumer.ReaderBatchSize
        };

        await _consumerDao.ReadMessagesAsync(readMessagesRequest, command.MessagesBuffer);
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var setLoadedCommand = new SetLoadedMessagesCommand
        {
            Consumer = command.Consumer,
            PartitionNum = command.PartitionNum,
            UsedOffset = command.Offset,
            MessagesBuffer = command.MessagesBuffer,
        };
        
        await _resultCommandBus.Send(setLoadedCommand);
    }

    private async Task HandleCommitOffset(CommitDaoCommand command, CancellationToken cancellationToken)
    {
        var isStillCaptured = await _consumerDao.CommitOffsetAsync(
            _context.ServerId, command.Consumer, command.PartitionNum, command.Offset);

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var setCommittedCommand = new SetCommittedCommand
        {
            Consumer = command.Consumer,
            PartitionNum = command.PartitionNum,
            IsStillCaptured = isStillCaptured,
            Offset = command.Offset,
        };
        
        await _resultCommandBus.Send(setCommittedCommand);
    }
}