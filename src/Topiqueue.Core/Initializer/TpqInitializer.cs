using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Topiqueue.Core.Configuration;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Dao;

namespace Topiqueue.Core.Initializer;

internal class TpqInitializer : ITpqInitializer
{
    private readonly ITpqDao _dao;
    private readonly ILogger<TpqInitializer> _logger;
    
    private readonly ITopicsRegistry _topics;
    private readonly IReadOnlyList<TpqConsumerSettings> _consumers;
    private readonly TpqBackgroundServiceSettings _backgroundServiceSettings;
    private readonly string _serverId;

    public TpqInitializer(
        ITpqDao dao,
        ILogger<TpqInitializer> logger,
        ITopicsRegistry topics,
        IReadOnlyList<TpqConsumerSettings> consumers,
        TpqBackgroundServiceSettings backgroundServiceSettings,
        string serverId)
    {
        _dao = dao;
        _logger = logger;
        
        _topics = topics;
        _consumers = consumers;
        _backgroundServiceSettings = backgroundServiceSettings;
        _serverId = serverId;
    }

    public void Initialize(bool runDbMigrations = true)
    {
        if (runDbMigrations)
        {
            _dao.Migrator.Migrate();
        }

        foreach (var topic in _topics.GetAll())
        {
            var ensureTopicCreatedResult = _dao.TopicsDao.EnsureTopicCreated(
                topic.TopicName,
                topic.PartitionsCount,
                topic.RetentionInterval);

            if (ensureTopicCreatedResult.CreatedNow)
            {
                _logger.LogInformation("Topic {TopicName} has been created", topic.TopicName);
            }
            // todo: check existing topic params are equal to specified
            
            var ensureHasSegmentResult = _dao.TopicsDao.EnsureTopicHasSegment(
                topic.TopicName,
                _backgroundServiceSettings.SegmentBoundaryThreshold);

            if (ensureHasSegmentResult.CreatedSegmentStart.HasValue &&
                ensureHasSegmentResult.CreatedSegmentEnd.HasValue)
            {
                _logger.LogInformation("Segment for topic {TopicName} has been created, start = {SegmentStart}, end = {SegmentEnd}", 
                    topic.TopicName, ensureHasSegmentResult.CreatedSegmentStart.Value, ensureHasSegmentResult.CreatedSegmentEnd.Value);
            }
        }
        
        _dao.ServersDao.AnnounceServer(_serverId,  _consumers);
        
        var deletedServerIds = new List<string>();
        _dao.ServersDao.DeleteOutdated(_backgroundServiceSettings.HeartbeatOutdatedThreshold, deletedServerIds);
        foreach (var deletedServerId in deletedServerIds)
        {
            _logger.LogInformation("Outdated server {ServerId} has been deleted", deletedServerId);
        }

        _dao.ConsumerDao.AnnounceConsumers(_consumers, _topics);
    }
}