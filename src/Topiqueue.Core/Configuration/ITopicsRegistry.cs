using Topiqueue.Core.Configuration.Settings;

namespace Topiqueue.Core.Configuration;

internal interface ITopicsRegistry
{
    TpqTopicSettings? GetOrDefault(string topicName);
    TpqTopicSettings GetRequired(string topicName);
}