namespace Topiqueue.Core.Messages.Interfaces;

internal interface IPartitionNumCalculator
{
    int GetPartitionNum(string? partitionKey, int partitionsCount);
}