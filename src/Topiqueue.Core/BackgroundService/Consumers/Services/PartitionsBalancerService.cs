using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Dao;
using Topiqueue.Core.Helpers;

namespace Topiqueue.Core.BackgroundService.Consumers.Services;

internal class PartitionsBalancerService : IPartitionsBalancerService
{
    private readonly ITpqServersDao _serversDao;
    private readonly ITimerService _timerService;
    private readonly IConsumersContext _context;
    private readonly ILogger<PartitionsBalancerService> _logger;
    private readonly TpqBackgroundServiceSettings _settings;

    public PartitionsBalancerService(
        IConsumersContext context,
        ITpqServersDao serversDao,
        ITimerService timerService,
        ILogger<PartitionsBalancerService> logger,
        TpqBackgroundServiceSettings settings)
    {
        _serversDao = serversDao;
        _timerService = timerService;
        _context = context;
        _logger = logger;
        _settings = settings;
    }

    public void Run(CancellationToken cancellationToken)
    {
        foreach (var consumer in _context.Consumers)
        {
            _context.CommandBus.SendTryCapturePartitions(consumer, consumer.TryCapturePartitionsOnStart);
        }
        
        _ = Task.Run(async () => await CheckBalanceProcess(cancellationToken), cancellationToken);
    }

    private async Task CheckBalanceProcess(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _timerService.TryDelay(_settings.CheckPartitionsBalanceInterval, cancellationToken);
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
                _logger.LogError(e, "Error int PartitionsBalancerService. The next attempt will be in {DbErrorPause}",
                    _settings.DbErrorPause);
                
                await _timerService.TryDelay(_settings.DbErrorPause, cancellationToken);
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
                _settings.HeartbeatOutdatedThreshold);
                
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
                
            var totalPartitions = _context.Topics.Get(consumer.TopicName).PartitionsCount;
            var capturedPartitions = _context.GetCapturedPartitionsCount(consumer);
                
            var fairCount = (int)Math.Ceiling((decimal)totalPartitions / totalServers);
            if (capturedPartitions < fairCount)
            {
                var partitionsToCapture = fairCount - capturedPartitions;
                await _context.CommandBus.SendTryCapturePartitions(consumer, partitionsToCapture);
            }
            else if (capturedPartitions > fairCount)
            {
                
                var partitionsToRelease = capturedPartitions - fairCount;
                await _context.CommandBus.SendReleasePartitions(consumer, partitionsToRelease);
            }
        }        
    }
}