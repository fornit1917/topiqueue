using System.Collections.Generic;
using System.Threading.Tasks;
using Topiqueue.Core.Configuration;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Dao.Models;
using Topiqueue.Core.Messages.Models;

namespace Topiqueue.Core.Dao;

public interface ITpqConsumerDao
{
    void AnnounceConsumers(IReadOnlyList<TpqConsumerSettings> consumers, ITopicsRegistry topicsRegistry);
    
    Task<int> GetCapturedPartitionsCount(string serverId, TpqConsumerSettings consumer);
    
    Task<List<CapturedPartition>> CapturePartitionsAsync(string serverId, TpqConsumerSettings consumer, int partitionCount);

    Task ReleasePartitionsAsync(string serverId, TpqConsumerSettings consumer, IReadOnlyList<int> partitionNums);
    
    Task ReadMessagesAsync(ReadMessagesRequest request, List<TpqMessageModel> result);
    
    Task<bool> CommitOffsetAsync(string serverId, TpqConsumerSettings consumer, int partitionNum, PartitionOffset offset);
}