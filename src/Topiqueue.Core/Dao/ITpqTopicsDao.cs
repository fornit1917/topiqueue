using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Topiqueue.Core.Dao.Models;

namespace Topiqueue.Core.Dao;

public interface ITpqTopicsDao
{
    EnsureTopicCreatedResult EnsureTopicCreated(string topicName, int partitionsCount, TimeSpan retentionInterval);
    
    EnsureHasSegmentResult EnsureTopicHasSegment(string topicName, TimeSpan threshold);
    Task<EnsureHasSegmentResult> EnsureTopicHasSegmentAsync(string topicName, TimeSpan threshold);
    
    Task TryDeleteOutdatedSegmentsAsync(string topicName, TimeSpan threshold, List<DeletedSegment> output);
}