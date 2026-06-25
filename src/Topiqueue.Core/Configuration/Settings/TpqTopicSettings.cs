using System;
using Topiqueue.Core.Helpers;

namespace Topiqueue.Core.Configuration.Settings;

public class TpqTopicSettings
{
    public string TopicName { get; }
    public int PartitionsCount { get; }
    public TimeSpan RetentionInterval { get; }

    public TpqTopicSettings(string topicName, int partitionsCount, TimeSpan retentionInterval)
    {
        TopicName = topicName
            .EnsureNotEmpty(nameof(topicName));
        
        PartitionsCount = partitionsCount.EnsureGreaterThan(0, nameof(partitionsCount));
        
        RetentionInterval = retentionInterval.EnsureGreaterThan(TimeSpan.Zero, nameof(retentionInterval));
    }
}