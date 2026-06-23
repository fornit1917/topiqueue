using System.Threading.Tasks;
using Topiqueue.Core.Messages;

namespace Topiqueue.Core.Producer;

public interface ITpqProducer
{
    void Produce<T>(string topicName, T data, string? partitionKey = null) where T : ITpqMessageData;
    Task ProduceAsync<T>(string topicName, T data, string? partitionKey = null) where T : ITpqMessageData; 
}