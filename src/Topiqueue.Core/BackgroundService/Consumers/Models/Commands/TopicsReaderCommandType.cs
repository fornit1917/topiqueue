namespace Topiqueue.Core.BackgroundService.Consumers.Models.Commands;

internal enum TopicsReaderCommandType
{
    TryCapturePartitions,
    ReleasePartitions,
}