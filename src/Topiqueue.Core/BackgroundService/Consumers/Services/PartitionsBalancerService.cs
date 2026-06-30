using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces;
using Topiqueue.Core.BackgroundService.Consumers.Models.Commands;
using Topiqueue.Core.Dao;
using Topiqueue.Core.Helpers;

namespace Topiqueue.Core.BackgroundService.Consumers.Services;

internal class PartitionsBalancerService : IPartitionsBalancerService
{
    private readonly ITpqServersDao _serversDao;
    private readonly ITpqConsumerDao _consumersDao;
    private readonly ITimerService _timerService;
    private readonly ILogger<PartitionsBalancerService> _logger;
    private readonly IConsumersContext _context;

    public PartitionsBalancerService(
        ITpqServersDao serversDao,
        ITpqConsumerDao consumersDao,
        ITimerService timerService,
        ILogger<PartitionsBalancerService> logger,
        IConsumersContext context)
    {
        _serversDao = serversDao;
        _timerService = timerService;
        _consumersDao = consumersDao;
        _logger = logger;
        _context = context;
    }

    public void Run(CancellationToken cancellationToken)
    {
        foreach (var consumer in _context.Consumers)
        {
            var command = ConsumersCommand.CapturePartitions(consumer, consumer.TryCapturePartitionsOnStart);
            
            // todo: get rid of GetAwaiter
            _context.CommandsWriter.Write(command).GetAwaiter().GetResult();
        }
        
        _ = Task.Run(async () => await CheckBalanceProcess(cancellationToken), cancellationToken);
    }

    private async Task CheckBalanceProcess(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _timerService.TryDelay(_context.Settings.CheckPartitionsBalanceInterval, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }            
            
            try
            {
                await CheckBalance(cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error in PartitionsBalancerService. The next attempt will be in {DbErrorPause}",
                    _context.Settings.DbErrorPause);
                
                await _timerService.TryDelay(_context.Settings.DbErrorPause, cancellationToken);
            }
        }
    }

    private async Task CheckBalance(CancellationToken cancellationToken)
    {
        foreach (var consumer in _context.Consumers)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var totalServers = await _serversDao.GetServersCountInGroupAsync(consumer.TopicName, consumer.ConsumerGroupId,
                _context.Settings.HeartbeatOutdatedThreshold);
                
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
                
            var totalPartitions = _context.Topics.Get(consumer.TopicName).PartitionsCount;
            
            var capturedPartitions = await _consumersDao.GetCapturedPartitionsCount(_context.ServerId, consumer);
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
                
            var fairCount = (int)Math.Ceiling((decimal)totalPartitions / totalServers);
            if (capturedPartitions < fairCount)
            {
                var partitionsToCapture = fairCount - capturedPartitions;
                var captureCommand = ConsumersCommand.CapturePartitions(consumer, partitionsToCapture);
                await _context.CommandsWriter.Write(captureCommand);
            }
            else if (capturedPartitions > fairCount)
            {
                var partitionsToRelease = capturedPartitions - fairCount;
                var releaseCommand = ConsumersCommand.ReleasePartitions(consumer, partitionsToRelease);
                await _context.CommandsWriter.Write(releaseCommand);
            }
        }        
    }
}