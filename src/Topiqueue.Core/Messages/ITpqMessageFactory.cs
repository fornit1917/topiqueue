using Topiqueue.Core.Messages.Models;

namespace Topiqueue.Core.Messages;

public interface ITpqMessageFactory
{
    TpqCreateMessageModel Create<T>(string topicName, T data, string? partitionKey = null) where T : ITpqMessageData;
}