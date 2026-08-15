using System;
using Topiqueue.Core.Messages.Interfaces;

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

public class TpqMessageModel<T> where T : ITpqMessageData
{
    public required string TopicName { get; init; }
    public required int PartitionNum { get; init; }
    public required PartitionOffset Offset { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required string? PartitionKey { get; init; }
    public required string MessageType { get; init; }
    public required T Data { get; init; }
}