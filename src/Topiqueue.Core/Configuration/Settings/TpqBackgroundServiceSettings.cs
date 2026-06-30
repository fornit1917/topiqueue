using System;
using Topiqueue.Core.Helpers;

namespace Topiqueue.Core.Configuration.Settings;

public class TpqBackgroundServiceSettings
{
    private readonly TimeSpan _dbErrorPause = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _rotateSegmentsInterval = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _segmentBoundaryThreshold = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _heartbeatOutdatedThreshold = TimeSpan.FromSeconds(180);
    private readonly TimeSpan _checkPartitionsBalanceInterval = TimeSpan.FromSeconds(5);
    private readonly int _dbQueryExecutorWorkers = 1;
    private readonly int _messagesHandlerWorkers = 1;

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

    public TimeSpan HeartbeatInterval
    {
        get => _heartbeatInterval;
        init => _heartbeatInterval = value.EnsureGreaterThan(TimeSpan.Zero, nameof(HeartbeatInterval));
    }

    public TimeSpan HeartbeatOutdatedThreshold
    {
        get => _heartbeatOutdatedThreshold;
        init => _heartbeatOutdatedThreshold = value.EnsureGreaterThan(TimeSpan.Zero, nameof(HeartbeatOutdatedThreshold));
    }

    public int DbQueryExecutorWorkers
    {
        get => _dbQueryExecutorWorkers;
        init => _dbQueryExecutorWorkers = value.EnsureGreaterThan(0, nameof(DbQueryExecutorWorkers));
    }

    public int MessagesHandlerWorkers
    {
        get => _messagesHandlerWorkers;
        init => _messagesHandlerWorkers = value.EnsureGreaterThan(0, nameof(MessagesHandlerWorkers));
    }

    public TimeSpan CheckPartitionsBalanceInterval
    {
        get => _checkPartitionsBalanceInterval;
        init => _checkPartitionsBalanceInterval = value.EnsureGreaterThan(TimeSpan.Zero, nameof(CheckPartitionsBalanceInterval));
    }
}