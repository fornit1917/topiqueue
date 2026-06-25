using System.Collections.Generic;
using System.Threading.Tasks;
using Topiqueue.Core.Configuration;
using Topiqueue.Core.Configuration.Settings;

namespace Topiqueue.Core.Dao;

public interface ITpqConsumerDao
{
    void AnnounceConsumers(IReadOnlyList<TpqConsumerSettings> consumers, ITopicsRegistry topicsRegistry);
    
    Task TryCapturePartitionsAsync(string serverId, TpqConsumerSettings consumer, int partitionCount,
        List<int> capturedPartitions);
    
    Task ReleasePartitionsAsync(TpqConsumerSettings consumer, int partitionCount);
}