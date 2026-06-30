using System.Collections.Generic;
using Topiqueue.Core.Configuration.Settings;

namespace Topiqueue.Core.BackgroundService.Consumers.Models.Commands;

internal struct DaoCommand
{
    public DaoCommandType Type { get; init; }
    public TpqConsumerSettings Consumer { get; init; }
    
    public int PartitionsCount { get; init; }
    public IReadOnlyList<int>? PartitionNums { get; init; }

    public static DaoCommand CapturePartitions(TpqConsumerSettings consumer, int partitionsCount)
    {
        return new DaoCommand
        {
            Type = DaoCommandType.CapturePartitions,
            Consumer = consumer,
            PartitionsCount = partitionsCount,
        };
    }

    public static DaoCommand ReleasePartitions(TpqConsumerSettings consumer, IReadOnlyList<int> partitionNums)
    {
        return new DaoCommand
        {
            Type = DaoCommandType.ReleasePartitions,
            Consumer = consumer,
            PartitionNums = partitionNums
        };
    }
}