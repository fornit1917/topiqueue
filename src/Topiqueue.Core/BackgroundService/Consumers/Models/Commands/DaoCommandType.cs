namespace Topiqueue.Core.BackgroundService.Consumers.Models.Commands;

internal enum DaoCommandType
{
    CapturePartitions,
    ReleasePartitions,
}