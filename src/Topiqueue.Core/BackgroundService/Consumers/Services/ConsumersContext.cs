using System.Collections.Frozen;
using System.Collections.Generic;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces;
using Topiqueue.Core.BackgroundService.Consumers.Models;
using Topiqueue.Core.Configuration;
using Topiqueue.Core.Configuration.Settings;

namespace Topiqueue.Core.BackgroundService.Consumers.Services;

internal class ConsumersContext : IConsumersContext
{
    private readonly FrozenDictionary<string, FrozenDictionary<string, GroupState>> _consumersState;
    
    public ITopicsRegistry Topics { get; }
    public IReadOnlyList<TpqConsumerSettings> Consumers { get; }
    public IConsumersCommandBus CommandBus { get; }

    public ConsumersContext(
        ITopicsRegistry topics,
        IReadOnlyList<TpqConsumerSettings> consumers,
        IConsumersCommandBus commandBus)
    {
        Topics = topics;
        Consumers = consumers;
        CommandBus = commandBus;
        
        var groupsByTopic = new Dictionary<string, Dictionary<string, GroupState>>();
        foreach (var consumer in consumers)
        {
            var topic = topics.Get(consumer.TopicName);
            if (!groupsByTopic.ContainsKey(topic.TopicName))
            {
                groupsByTopic[consumer.TopicName] = new Dictionary<string, GroupState>();
            }

            var groupState = new GroupState(consumer.TopicName, consumer.ConsumerGroupId, topic.PartitionsCount);
            groupsByTopic[consumer.TopicName][consumer.ConsumerGroupId] = groupState;
        }

        _consumersState = groupsByTopic
            .ToFrozenDictionary(x => x.Key, x => x.Value.ToFrozenDictionary());
    }

    public int GetCapturedPartitionsCount(TpqConsumerSettings consumer)
        => _consumersState[consumer.TopicName][consumer.ConsumerGroupId].TotalCaptured;

    public bool TrySetCaptured(TpqConsumerSettings consumer, int partitionNum)
        => _consumersState[consumer.TopicName][consumer.ConsumerGroupId].TrySetCaptured(partitionNum);

    public bool TrySetReleased(TpqConsumerSettings consumer, int partitionNum)
        => _consumersState[consumer.TopicName][consumer.ConsumerGroupId].TrySetReleased(partitionNum);
}