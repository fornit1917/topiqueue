using Topiqueue.Core.Configuration;
using Topiqueue.Core.Messages.Interfaces;
using Topiqueue.Core.Messages.Models;

namespace Topiqueue.Core.Messages.Services;

internal class MessageFactory : ITpqMessageFactory
{
    private readonly ITopicsRegistry _topicsRegistry;
    private readonly ITpqMessageDataSerializer _serializer;
    private readonly IPartitionNumCalculator _partitionNumCalculator;

    public MessageFactory(
        ITopicsRegistry topicsRegistry,
        ITpqMessageDataSerializer serializer,
        IPartitionNumCalculator partitionNumCalculator)
    {
        _topicsRegistry = topicsRegistry;
        _serializer = serializer;
        _partitionNumCalculator = partitionNumCalculator;
    }

    public TpqCreateMessageModel Create<T>(string topicName, T data, string? partitionKey = null) where T : ITpqMessageData
    {
        var topic = _topicsRegistry.Get(topicName);
        var createMessageModel = new TpqCreateMessageModel
        {
            TopicName = topicName,
            MessageType = T.GetMessageType(),
            PartitionKey = partitionKey,
            PartitionNum = _partitionNumCalculator.GetPartitionNum(partitionKey, topic.PartitionsCount),
            DataTxt = _serializer.SerializeToText(data),
        };
        return createMessageModel;
    }
}