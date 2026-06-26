using System;
using AwesomeAssertions;
using NSubstitute;
using Topiqueue.Core.Configuration;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Messages;
using Topiqueue.Core.Messages.Interfaces;
using Topiqueue.Core.Messages.Models;
using Topiqueue.Core.Messages.Services;
using Topiqueue.TestsUtils.Messages;

namespace Topiqueue.Tests.Core.Messages;

public class MessageFactoryTests
{
    private readonly ITopicsRegistry _topicsRegistry = Substitute.For<ITopicsRegistry>();
    private readonly ITpqMessageDataSerializer _serializer = Substitute.For<ITpqMessageDataSerializer>();
    private readonly IPartitionNumCalculator _partitionNumCalculator = Substitute.For<IPartitionNumCalculator>();
    
    private readonly MessageFactory _sut;

    public MessageFactoryTests()
    {
        _sut = new MessageFactory(_topicsRegistry, _serializer, _partitionNumCalculator);
    }

    [Fact]
    public void Create_CreatesMessage()
    {
        var messageData = new TestMessageData();
        var topic = new TpqTopicSettings("topicName", 8, TimeSpan.FromHours(24));
        var expectedSerializedData = "serialized";
        var expectedPartitionNum = 4;
        var partitionKey = "partitionKey";
        _serializer.SerializeToText(messageData).Returns(expectedSerializedData);
        _topicsRegistry.Get(topic.TopicName).Returns(topic);
        _partitionNumCalculator.GetPartitionNum(partitionKey, topic.PartitionsCount).Returns(expectedPartitionNum);
        
        var result = _sut.Create(topic.TopicName, messageData, partitionKey);

        result.Should().BeEquivalentTo(new TpqCreateMessageModel
        {
            TopicName = topic.TopicName,
            DataTxt = expectedSerializedData,
            MessageType = TestMessageData.GetMessageType(),
            PartitionKey = partitionKey,
            PartitionNum = expectedPartitionNum,
        });
    }
}