namespace Topiqueue.Core.Messages;

internal interface IPartitionNumCalculator
{
    int GetPartitionNum(string? partitionKey, int partitionsCount);
}