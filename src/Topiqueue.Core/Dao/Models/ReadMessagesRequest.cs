using Topiqueue.Core.Messages.Models;

namespace Topiqueue.Core.Dao.Models;

public struct ReadMessagesRequest
{
    public string TopicName { get; init; }
    public int PartitionNum { get; init; }
    public PartitionOffset Offset { get; init; }
    public int Limit { get; init; }
}