using System;
using System.Threading;
using Topiqueue.Core.BackgroundService.RotateSegments;

namespace Topiqueue.Core.BackgroundService;

internal class TpqBackgroundService : ITpqBackgroundService
{
    private readonly IRotateSegmentsService _rotateSegmentsService;
    
    private readonly CancellationTokenSource _cancellationTokenSource;

    public TpqBackgroundService(IRotateSegmentsService rotateSegmentsService)
    {
        _rotateSegmentsService = rotateSegmentsService;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public void StartBackgroundService()
    {
        if (_cancellationTokenSource.IsCancellationRequested)
        {
            throw new InvalidOperationException("The background service has been stopped and can not be started again.");
        }
        
        _rotateSegmentsService.Run(_cancellationTokenSource.Token);
    }

    public void SendStopSignal()
    {
        _cancellationTokenSource.Cancel();
    }
}