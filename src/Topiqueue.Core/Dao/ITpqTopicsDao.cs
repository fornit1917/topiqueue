using System;
using Topiqueue.Core.Dao.Models;

namespace Topiqueue.Core.Dao;

public interface ITpqTopicsDao
{
    EnsureTopicCreatedResult EnsureTopicCreated(string topicName, int partitionsCount, TimeSpan retentionInterval);
    EnsureHasSegmentResult EnsureTopicHasSegment(string topicName, TimeSpan threshold);
}