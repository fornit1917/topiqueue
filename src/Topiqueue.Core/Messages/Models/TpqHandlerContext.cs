namespace Topiqueue.Core.Messages.Models;

public class TpqHandlerContext
{
    public required string TopicName { get; init; }
    public required int PartitionNum { get; init; }
}