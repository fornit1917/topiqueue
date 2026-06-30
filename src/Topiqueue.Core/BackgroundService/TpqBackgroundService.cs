using System;
using System.Threading;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces;
using Topiqueue.Core.BackgroundService.Heartbeat;
using Topiqueue.Core.BackgroundService.SegmentsRotation;

namespace Topiqueue.Core.BackgroundService;

internal class TpqBackgroundService : ITpqBackgroundService
{
    private readonly ISegmentsRotationService _segmentsRotationService;
    private readonly IHeartbeatService _heartbeatService;
    private readonly IPartitionsBalancerService _partitionsBalancerService;
    private readonly IConsumersDispatcherService _consumersDispatcherService;
    private readonly IConsumersDaoService _consumersDaoService;
    
    private readonly CancellationTokenSource _cancellationTokenSource;
    
    public TpqBackgroundService(
        ISegmentsRotationService segmentsRotationService,
        IHeartbeatService heartbeatService,
        IPartitionsBalancerService partitionsBalancerService,
        IConsumersDispatcherService consumersDispatcherService,
        IConsumersDaoService consumersDaoService)
    {
        _segmentsRotationService = segmentsRotationService;
        _heartbeatService = heartbeatService;
        _partitionsBalancerService = partitionsBalancerService;
        _consumersDispatcherService = consumersDispatcherService;
        _consumersDaoService = consumersDaoService;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public void StartBackgroundService()
    {
        if (_cancellationTokenSource.IsCancellationRequested)
        {
            throw new InvalidOperationException("The background service has been stopped and can not be started again.");
        }
        
        _segmentsRotationService.Run(_cancellationTokenSource.Token);
        _heartbeatService.Run(_cancellationTokenSource.Token);
        _partitionsBalancerService.Run(_cancellationTokenSource.Token);
        _consumersDaoService.Run(_cancellationTokenSource.Token);
        _consumersDispatcherService.Run(_cancellationTokenSource.Token);
    }

    public void SendStopSignal()
    {
        _cancellationTokenSource.Cancel();
    }
}