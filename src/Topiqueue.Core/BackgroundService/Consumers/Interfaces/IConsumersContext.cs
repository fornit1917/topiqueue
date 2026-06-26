using System.Collections.Generic;
using Topiqueue.Core.Configuration;
using Topiqueue.Core.Configuration.Settings;

namespace Topiqueue.Core.BackgroundService.Consumers.Interfaces;

internal interface IConsumersContext
{
    ITopicsRegistry Topics { get; }
    IReadOnlyList<TpqConsumerSettings> Consumers { get; }
    IConsumersCommandBus CommandBus { get; }
    
    int GetCapturedPartitionsCount(TpqConsumerSettings consumer);
    
    bool TrySetCaptured(TpqConsumerSettings consumer, int partitionNum);
    
    bool TrySetReleased(TpqConsumerSettings consumer, int partitionNum);
}