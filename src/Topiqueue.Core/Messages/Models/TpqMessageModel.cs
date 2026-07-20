using System;

namespace Topiqueue.Core.Messages.Models;

public class TpqMessageModel
{
    public string TopicName { get; init; } = "";
    public int PartitionNum { get; init; }
    public long TxId { get; init; }
    public long SeqId { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? PartitionKey { get; init; }
    public string MessageType { get; init; } = "";
    public string? DataTxt { get; init; }
}