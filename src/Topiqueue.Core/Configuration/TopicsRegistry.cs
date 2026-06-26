using System.Collections.Frozen;
using System.Collections.Generic;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Exceptions;

namespace Topiqueue.Core.Configuration;

internal class TopicsRegistry : ITopicsRegistry
{
    private readonly FrozenDictionary<string, TpqTopicSettings> _topicsByName;

    public TopicsRegistry(IReadOnlyList<TpqTopicSettings> topics)
    {
        _topicsByName = topics.ToFrozenDictionary(x => x.TopicName, x => x);
    }

    public TpqTopicSettings? GetOrDefault(string topicName)
    {
        return _topicsByName.GetValueOrDefault(topicName);
    }

    public TpqTopicSettings Get(string topicName)
    {
        return _topicsByName.TryGetValue(topicName, out var topic) 
            ? topic 
            : throw new UnknownTopicException(topicName);
    }

    public IEnumerable<TpqTopicSettings> GetAll()
    {
        return _topicsByName.Values;
    }
}