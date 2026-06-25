namespace Topiqueue.Core.BackgroundService.Consumers.Models;

internal enum TopicsReaderCommandType
{
    TryCapturePartitions,
    ReleasePartitions,
}