using System.Collections.Frozen;
using System.Collections.Generic;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces;
using Topiqueue.Core.BackgroundService.Consumers.Models;
using Topiqueue.Core.Configuration;
using Topiqueue.Core.Configuration.Settings;

namespace Topiqueue.Core.BackgroundService.Consumers.Services;

internal class PartitionsRegistry : IPartitionsRegistry
{
    private readonly FrozenDictionary<string, FrozenDictionary<string, PartitionState[]>> _paritions;

    public PartitionsRegistry(ITopicsRegistry topics, IReadOnlyList<TpqConsumerSettings> consumers)
    {
        var groupsByTopic = new Dictionary<string, Dictionary<string, PartitionState[]>>();
        foreach (var consumer in consumers)
        {
            var topic = topics.Get(consumer.TopicName);
            if (!groupsByTopic.ContainsKey(topic.TopicName))
            {
                groupsByTopic[consumer.TopicName] = new Dictionary<string, PartitionState[]>();
            }
            
            var partitions = new PartitionState[topic.PartitionsCount];
            for (int i = 0; i < partitions.Length; i++)
            {
                partitions[i] = new PartitionState
                {
                    TopicName = consumer.TopicName,
                    ConsumerGroupId = consumer.ConsumerGroupId,
                    PartitionNum = i
                };
            }
            
            groupsByTopic[consumer.TopicName][consumer.ConsumerGroupId] = partitions;
        }

        _paritions = groupsByTopic
            .ToFrozenDictionary(x => x.Key, x => x.Value.ToFrozenDictionary());
    }
    
    public PartitionState Get(TpqConsumerSettings consumer, int partitionNum)
    {
        return _paritions[consumer.TopicName][consumer.ConsumerGroupId][partitionNum];
    }

    public IEnumerable<PartitionState> GetCaptured()
    {
        foreach (var topicName in _paritions.Keys)
        {
            foreach (var consumerGroupId in _paritions[topicName].Keys)
            {
                foreach (var partition in _paritions[topicName][consumerGroupId])
                {
                    if (partition.Captured)
                    {
                        yield return partition;    
                    }
                }
            }
        }
    }
}