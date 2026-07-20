using System;
using Topiqueue.Core.Messages.Models;

namespace Topiqueue.Core.Dao.Models;

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