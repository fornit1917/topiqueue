using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Topiqueue.Core.Configuration;
using Topiqueue.Core.Configuration.Settings;

namespace Topiqueue.Core.Dao;

public interface ITpqServersDao
{
    void AnnounceServer(string serverId, IReadOnlyList<TpqConsumerSettings> consumers);
    
    void DeleteOutdated(TimeSpan threshold, List<string> deletedServerIds);
    Task DeleteOutdatedAsync(TimeSpan threshold, List<string> deletedServerIds);

    Task UpsertHeartbeatTsAsync(string serverId, IReadOnlyList<TpqConsumerSettings> consumers);
}