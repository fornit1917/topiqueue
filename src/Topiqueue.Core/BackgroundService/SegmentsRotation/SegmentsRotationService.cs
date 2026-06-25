using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Dao;
using Topiqueue.Core.Dao.Models;
using Topiqueue.Core.Helpers;

namespace Topiqueue.Core.BackgroundService.SegmentsRotation;

internal class SegmentsRotationService : ISegmentsRotationService
{
    private readonly ITpqTopicsDao _tpqTopicsDao;
    private readonly ITimerService _timerService;
    private readonly ILogger<SegmentsRotationService> _logger;
    private readonly IReadOnlyList<TpqTopicSettings> _tpqTopicSettings;
    private readonly TpqBackgroundServiceSettings _settings;
    
    private readonly List<DeletedSegment> _deletedSegments;

    public SegmentsRotationService(
        ITpqTopicsDao tpqTopicsDao,
        ITimerService timerService,
        ILogger<SegmentsRotationService> logger,
        IReadOnlyList<TpqTopicSettings> tpqTopicSettings,
        TpqBackgroundServiceSettings settings)
    {
        _tpqTopicsDao = tpqTopicsDao;
        _timerService = timerService;
        
        _tpqTopicSettings = tpqTopicSettings;
        _settings = settings;
        _logger = logger;

        _deletedSegments = new List<DeletedSegment>(capacity: 4);
    }

    public void Run(CancellationToken cancellationToken)
    {
        _ = Task.Run(async () => await CheckSegments(cancellationToken), cancellationToken);
    }

    private async Task CheckSegments(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                foreach (var topic in _tpqTopicSettings)
                {
                    var ensureHasSegmentResult = await _tpqTopicsDao.EnsureTopicHasSegmentAsync(
                        topic.TopicName, _settings.SegmentBoundaryThreshold);
                    
                    if (ensureHasSegmentResult.CreatedSegmentStart.HasValue &&
                        ensureHasSegmentResult.CreatedSegmentEnd.HasValue)
                    {
                        _logger.LogInformation("Segment for topic {TopicName} has been created, start = {SegmentStart}, end = {SegmentEnd}", 
                            topic.TopicName, ensureHasSegmentResult.CreatedSegmentStart.Value, ensureHasSegmentResult.CreatedSegmentEnd.Value);                        
                    }

                    await _tpqTopicsDao.TryDeleteOutdatedSegmentsAsync(topic.TopicName, 
                        _settings.SegmentBoundaryThreshold, _deletedSegments);

                    foreach (var deletedSegment in _deletedSegments)
                    {
                        _logger.LogInformation("Outdated segment for topic {TopicName} has been deleted, start = {SegmentStart}, end = {SegmentEnd}",
                            topic.TopicName, deletedSegment.SegmentStart, deletedSegment.SegmentEnd);
                    }
                }
                
                await _timerService.TryDelay(_settings.RotateSegmentsInterval, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error in CheckSegmentsService. The next attempt will be in {DbErrorPause}",
                    _settings.DbErrorPause);
            }
            
        }
    }
}