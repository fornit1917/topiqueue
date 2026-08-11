using System.Collections.Generic;
using System.Threading.Tasks;
using Topiqueue.Core.Messages.Interfaces;
using Topiqueue.Core.Messages.Models;

namespace Topiqueue.Core.Producer;

public interface ITpqProducer
{
    void Produce<T>(string topicName, T data, string? partitionKey = null) where T : ITpqMessageData;
    Task ProduceAsync<T>(string topicName, T data, string? partitionKey = null) where T : ITpqMessageData;
    
    void ProduceBatch(IReadOnlyList<TpqCreateMessageModel> messages);
    Task ProduceBatchAsync(IReadOnlyList<TpqCreateMessageModel> messages);
}