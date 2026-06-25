using System.Threading.Tasks;
using Topiqueue.Core.Messages;
using Topiqueue.Core.Messages.Interfaces;

namespace Topiqueue.Core.Producer;

public interface ITpqProducer
{
    void Produce<T>(string topicName, T data, string? partitionKey = null) where T : ITpqMessageData;
    Task ProduceAsync<T>(string topicName, T data, string? partitionKey = null) where T : ITpqMessageData; 
}