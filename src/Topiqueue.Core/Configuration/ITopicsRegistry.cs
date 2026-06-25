using System.Collections.Generic;
using Topiqueue.Core.Configuration.Settings;

namespace Topiqueue.Core.Configuration;

public interface ITopicsRegistry
{
    TpqTopicSettings? GetOrDefault(string topicName);
    TpqTopicSettings GetRequired(string topicName);
    IEnumerable<TpqTopicSettings> GetAll();
}