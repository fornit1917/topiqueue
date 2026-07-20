namespace Topiqueue.Core.BackgroundService.Consumers.Commands.Dao;

internal enum DaoCommandType
{
    Unknown,
    CapturePartitions,
    ReleasePartitions,
    LoadMessages,
    CommitOffset,
}