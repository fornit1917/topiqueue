using System;

namespace Topiqueue.Core.Dao.Models;

public class CapturedPartition
{
    public int PartitionNum { get; init; }
    public string LastProcessedTxId { get; init; } = "0";
    public long LastProcessedSeqId { get; init; }
    public DateTime LastProcessedCreatedAt { get; init; }
}