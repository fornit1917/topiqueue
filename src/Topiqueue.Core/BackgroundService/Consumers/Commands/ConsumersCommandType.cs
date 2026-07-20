namespace Topiqueue.Core.BackgroundService.Consumers.Commands;

internal enum ConsumersCommandType
{
    Unknown,
    
    CapturePartitions,
    SetPartitionCaptured,
    
    ReleasePartitions,
    SetPartitionReleased,
    
    LoadMessages,
    SetLoadedMessages,
    
    Commit,
    SetCommitted,
}