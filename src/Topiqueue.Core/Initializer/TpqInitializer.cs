using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Dao;

namespace Topiqueue.Core.Initializer;

internal class TpqInitializer : ITpqInitializer
{
    private readonly ITpqDbMigrator _dbMigrator;
    private readonly ITpqTopicsDao _topicsDao;
    private readonly ILogger<TpqInitializer> _logger;
    
    private readonly IReadOnlyList<TpqTopicSettings> _topics;
    private readonly TpqBackgroundServiceSettings _backgroundServiceSettings;

    public TpqInitializer(
        ITpqDbMigrator dbMigrator,
        ITpqTopicsDao topicsDao,
        ILogger<TpqInitializer> logger,
        IReadOnlyList<TpqTopicSettings> topics,
        TpqBackgroundServiceSettings backgroundServiceSettings)
    {
        _dbMigrator = dbMigrator;
        _topicsDao = topicsDao;
        _logger = logger;
        
        _topics = topics;
        _backgroundServiceSettings = backgroundServiceSettings;
    }

    public void Initialize(bool runDbMigrations = true)
    {
        if (runDbMigrations)
        {
            _dbMigrator.Migrate();
        }

        foreach (var topic in _topics)
        {
            var ensureTopicCreatedResult = _topicsDao.EnsureTopicCreated(
                topic.TopicName,
                topic.PartitionsCount,
                topic.RetentionInterval);

            if (ensureTopicCreatedResult.CreatedNow)
            {
                _logger.LogInformation("Topic {TopicName} has been created", topic.TopicName);
            }
            // todo: check existing topic params are equal to specified
            
            var ensureHasSegmentResult = _topicsDao.EnsureTopicHasSegment(
                topic.TopicName,
                _backgroundServiceSettings.SegmentBoundaryThreshold);

            if (ensureHasSegmentResult.CreatedSegmentStart.HasValue &&
                ensureHasSegmentResult.CreatedSegmentEnd.HasValue)
            {
                _logger.LogInformation("Segment for topic {TopicName} has been created, start = {SegmentStart}, end = {SegmentEnd}", 
                    topic.TopicName, ensureHasSegmentResult.CreatedSegmentStart.Value, ensureHasSegmentResult.CreatedSegmentEnd.Value);
            }
            
        }
    }
}