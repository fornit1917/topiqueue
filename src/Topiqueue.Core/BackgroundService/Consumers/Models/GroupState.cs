using System.Threading;

namespace Topiqueue.Core.BackgroundService.Consumers.Models;

internal class GroupState
{
    private readonly string _topicName;
    private readonly string _groupName;
    private readonly PartitionState[] _partitions;

    private int _totalCaptured;
    
    public int TotalCaptured => Volatile.Read(ref _totalCaptured);

    public GroupState(string topicName, string groupName, int partitionsCount)
    {
        _topicName = topicName;
        _groupName = groupName;
        _partitions = new PartitionState[partitionsCount];
        for (int i = 0; i < _partitions.Length; i++)
        {
            _partitions[i] = new PartitionState();
        }
        
        _totalCaptured = 0;
    }

    public bool TrySetCaptured(int partitionNum)
    {
        if (_partitions[partitionNum].TrySetCaptured())
        {
            Interlocked.Increment(ref _totalCaptured);
            return true;
        }

        return false;
    }
    
    public bool TrySetReleased(int partitionNum)
    {
        if (_partitions[partitionNum].TrySetReleased())
        {
            Interlocked.Decrement(ref _totalCaptured);
            return true;
        }

        return false;
    }
}