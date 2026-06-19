using System;

namespace Topiqueue.Core.Dao.Models;

public class EnsureTopicCreatedResult
{
    public string TopicName { get; init; } = "";
    public int TopicSeqId { get; init; }
    public int PartitionsCount { get; init; }
    public TimeSpan RetentionInterval { get; init; }
    public bool CreatedNow { get; init; }
}