using System.Collections.Generic;
using System.Linq;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Dao.Models;
using Topiqueue.Core.Messages.Models;

namespace Topiqueue.Core.BackgroundService.Consumers.Models;

internal class PartitionState
{
    public required TpqConsumerSettings Consumer { get; init; }
    public required int PartitionNum  { get; init; }
    
    public PartitionOffset CommitedOffset { get; set; }
    public PartitionOffset? LastReadOffset { get; private set; }
    
    public bool Captured { get; private set; }
    public bool ReadInProgress { get; set; }
    public bool ReadOnPause { get; set; }
    public bool HandleInProgress { get; set; }
    public bool ReleaseRequested { get; set; }
    public int MessagesInCacheCount => _messagesCache.Count;
    
    private readonly Queue<TpqMessageModel> _messagesCache = new();
    
    public void SetReleased()
    {
        Captured = false;
        ReadInProgress = false;
        ReadOnPause = false;
        HandleInProgress = false;
        ReleaseRequested = false;
        LastReadOffset = null;
        _messagesCache.Clear();
    }

    public void SetCaptured(PartitionOffset offset)
    {
        Captured = true;
        ReadInProgress = false;
        ReadOnPause = false;
        HandleInProgress = false;
        ReleaseRequested = false;
        _messagesCache.Clear();
        LastReadOffset = null;
        CommitedOffset = offset;
    }

    public void AddMessagesToCache(IReadOnlyList<TpqMessageModel> messages)
    {
        foreach (var message in messages)
        {
            _messagesCache.Enqueue(message);
        }

        if (messages.Count > 0)
        {
            var lastMessage = messages.Last();
            LastReadOffset = new PartitionOffset(lastMessage);   
        }
    }

    public void DequeBatchForHandleFromCache(List<TpqMessageModel> outBuffer)
    {
        outBuffer.Clear();
        while (_messagesCache.Count > 0 && outBuffer.Count < Consumer.HandlerBatchSize)
        {
            outBuffer.Add(_messagesCache.Dequeue());
        }
    }
}