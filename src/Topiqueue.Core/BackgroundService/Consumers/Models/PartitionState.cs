namespace Topiqueue.Core.BackgroundService.Consumers.Models;

internal class PartitionState
{
    public required string TopicName { get; init; }
    public required string ConsumerGroupId { get; init; }
    public required int PartitionNum  { get; init; }
    
    public bool Captured { get; set; }
    public bool ReadInProgress { get; set; }
    public bool HandleInProgress { get; set; }
    public bool ReleaseRequested { get; set; }
}