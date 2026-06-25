using System;
using System.Threading;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces;
using Topiqueue.Core.BackgroundService.Heartbeat;
using Topiqueue.Core.BackgroundService.SegmentsRotation;
using Topiqueue.Core.Configuration.Settings;

namespace Topiqueue.Core.BackgroundService;

internal class TpqBackgroundService : ITpqBackgroundService
{
    private readonly ISegmentsRotationService _segmentsRotationService;
    private readonly IHeartbeatService _heartbeatService;
    private readonly ITopicsReaderService _topicsReaderService;
    
    private readonly CancellationTokenSource _cancellationTokenSource;
    
    public TpqBackgroundService(
        ISegmentsRotationService segmentsRotationService,
        IHeartbeatService heartbeatService,
        ITopicsReaderService topicsReaderService)
    {
        _segmentsRotationService = segmentsRotationService;
        _heartbeatService = heartbeatService;
        _topicsReaderService = topicsReaderService;
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
        _topicsReaderService.Run(_cancellationTokenSource.Token);
    }

    public void SendStopSignal()
    {
        _cancellationTokenSource.Cancel();
    }
}