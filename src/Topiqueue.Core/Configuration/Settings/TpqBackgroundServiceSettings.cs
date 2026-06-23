using System;
using Topiqueue.Core.Helpers;

namespace Topiqueue.Core.Configuration.Settings;

public class TpqBackgroundServiceSettings
{
    private readonly TimeSpan _dbErrorPause = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _rotateSegmentsInterval = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _segmentBoundaryThreshold = TimeSpan.FromMinutes(5);

    public TimeSpan RotateSegmentsInterval
    {
        get => _rotateSegmentsInterval; 
        init => _rotateSegmentsInterval = value.EnsureGreaterThan(TimeSpan.FromSeconds(1), nameof(RotateSegmentsInterval));
    }

    public TimeSpan SegmentBoundaryThreshold
    {
        get => _segmentBoundaryThreshold;
        init => _segmentBoundaryThreshold = value.EnsureGreaterThan(TimeSpan.FromMinutes(1), nameof(SegmentBoundaryThreshold));
    }
    
    public TimeSpan DbErrorPause
    {
        get => _dbErrorPause;
        init => _dbErrorPause = value.EnsureGreaterThan(TimeSpan.Zero, nameof(DbErrorPause));
    }
}