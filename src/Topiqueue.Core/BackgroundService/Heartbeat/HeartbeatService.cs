using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Dao;
using Topiqueue.Core.Helpers;

namespace Topiqueue.Core.BackgroundService.Heartbeat;

internal class HeartbeatService : IHeartbeatService
{
    private readonly ITpqServersDao _serversDao;
    private readonly ITimerService _timerService;
    private readonly ILogger<HeartbeatService> _logger;
    
    private readonly IReadOnlyList<TpqConsumerSettings> _consumers;
    private readonly TpqBackgroundServiceSettings _settings;
    private readonly string _serverId;

    public HeartbeatService(
        ITpqServersDao serversDao,
        ITimerService timerService,
        ILogger<HeartbeatService> logger,
        IReadOnlyList<TpqConsumerSettings> consumers,
        TpqBackgroundServiceSettings settings,
        string serverId)
    {
        _serversDao = serversDao;
        _timerService = timerService;
        _logger = logger;
        
        _consumers = consumers;
        _settings = settings;
        _serverId = serverId;
    }

    public void Run(CancellationToken cancellationToken)
    {
        _ = Task.Run(async () => await SendAndCheckHeartbeat(cancellationToken), cancellationToken);
    }

    private async Task SendAndCheckHeartbeat(CancellationToken cancellationToken)
    {
        var deletedServerIds = new List<string>();
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _serversDao.UpsertHeartbeatTsAsync(_serverId, _consumers);
                await _serversDao.DeleteOutdatedAsync(_settings.HeartbeatOutdatedThreshold, deletedServerIds);
                foreach (var deletedServerId in deletedServerIds)
                {
                    _logger.LogInformation("Outdated server {ServerId} has been deleted", deletedServerId);
                }
                deletedServerIds.Clear();
                await _timerService.TryDelay(_settings.HeartbeatInterval, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error in HeartbeatService. The next attempt will be in {DbErrorPause}",
                    _settings.DbErrorPause);
            }
        }
    }
}