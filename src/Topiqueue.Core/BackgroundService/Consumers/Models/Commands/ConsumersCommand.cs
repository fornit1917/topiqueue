using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Dao.Models;

namespace Topiqueue.Core.BackgroundService.Consumers.Models.Commands;

internal struct ConsumersCommand
{
    public ConsumerCommandType Type { get; init; }
    public TpqConsumerSettings Consumer { get; init; }
    
    public int PartitionsCount { get; init; }
    public CapturedPartition? CapturedPartition { get; init; }
    public int PartitionNum { get; init; }

    public static ConsumersCommand CapturePartitions(TpqConsumerSettings consumer, int partitionCount)
    {
        return new ConsumersCommand
        {
            Type = ConsumerCommandType.CapturePartitions,
            Consumer = consumer,
            PartitionsCount = partitionCount,
        };
    }

    public static ConsumersCommand SetPartitionCaptured(TpqConsumerSettings consumer, CapturedPartition capturedPartition)
    {
        return new ConsumersCommand
        {
            Type = ConsumerCommandType.SetPartitionCaptured,
            Consumer = consumer,
            CapturedPartition = capturedPartition,
        };
    }

    public static ConsumersCommand ReleasePartitions(TpqConsumerSettings consumer, int partitionCount)
    {
        return new ConsumersCommand
        {
            Type = ConsumerCommandType.ReleasePartitions,
            Consumer = consumer,
            PartitionsCount = partitionCount,
        };
    }

    public static ConsumersCommand SetPartitionReleased(TpqConsumerSettings consumer, int partitionNum)
    {
        return new ConsumersCommand
        {
            Type = ConsumerCommandType.SetPartitionReleased,
            Consumer = consumer,
            PartitionNum = partitionNum,
        };
    }
}