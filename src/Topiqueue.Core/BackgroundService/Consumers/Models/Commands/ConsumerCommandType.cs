namespace Topiqueue.Core.BackgroundService.Consumers.Models.Commands;

internal enum ConsumerCommandType
{
    CapturePartitions,
    SetPartitionCaptured,
    ReleasePartitions,
    SetPartitionReleased,
}