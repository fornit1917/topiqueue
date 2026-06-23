namespace Topiqueue.Core.Messages.Models;

public class TpqCreateMessageModel
{
    public string TopicName { get; internal init; } = "";
    public string? PartitionKey { get; internal init; }
    public int PartitionNum { get; internal init; }
    public string MessageType { get; internal init; } = "";
    public string? DataTxt { get; internal init; }
}