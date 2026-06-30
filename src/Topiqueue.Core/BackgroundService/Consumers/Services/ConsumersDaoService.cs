using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces;
using Topiqueue.Core.BackgroundService.Consumers.Models.Commands;
using Topiqueue.Core.Dao;
using Topiqueue.Core.Helpers;

namespace Topiqueue.Core.BackgroundService.Consumers.Services;

internal class ConsumersDaoService : IConsumersDaoService
{
    private readonly Channel<DaoCommand> _channel;
    private readonly ITpqConsumerDao _consumerDao;
    private readonly ITimerService _timerService;
    private readonly IConsumersContext _context;
    private readonly ILogger<ConsumersDaoService> _logger;

    public ConsumersDaoService(
        Channel<DaoCommand> channel,
        ITpqConsumerDao consumerDao,
        ITimerService timerService,
        IConsumersContext context,
        ILogger<ConsumersDaoService> logger)
    {
        _channel = channel;
        _consumerDao = consumerDao;
        _timerService = timerService;
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
                    DaoCommandType.CapturePartitions => HandleCapturePartitions(command, cancellationToken),
                    DaoCommandType.ReleasePartitions => HandleReleasePartitions(command, cancellationToken),
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

    private async Task HandleCapturePartitions(DaoCommand command, CancellationToken cancellationToken)
    {
        var capturedPartitions = await _consumerDao.CapturePartitionsAsync(_context.ServerId, command.Consumer, command.PartitionsCount);
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        foreach (var partition in capturedPartitions)
        {
            var setCapturedCommand = ConsumersCommand.SetPartitionCaptured(command.Consumer, partition);
            await _context.CommandsWriter.Write(setCapturedCommand);
        }
    }

    private async Task HandleReleasePartitions(DaoCommand command, CancellationToken cancellationToken)
    {
        var partitionNumsToRelease = command.PartitionNums ?? Array.Empty<int>();
        await _consumerDao.ReleasePartitionsAsync(_context.ServerId, command.Consumer, partitionNumsToRelease);
        foreach (var releasedPartition in partitionNumsToRelease)
        {
            var setReleasedCommand = ConsumersCommand.SetPartitionReleased(command.Consumer, releasedPartition); 
            await _context.CommandsWriter.Write(setReleasedCommand);
        }
    }
}