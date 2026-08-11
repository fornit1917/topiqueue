using System;

namespace Topiqueue.Core.Messages.Models;

public struct PartitionOffset
{
    public long TxId { get; init; }
    public long SeqId { get; init; }
    public DateTime CreatedAt { get; init; }

    public PartitionOffset()
    {
    }

    public PartitionOffset(TpqMessageModel message)
    {
        TxId = message.TxId;
        SeqId = message.SeqId;
        CreatedAt = message.CreatedAt;
    }
}