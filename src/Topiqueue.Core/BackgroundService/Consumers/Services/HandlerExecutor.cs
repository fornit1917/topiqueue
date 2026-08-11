using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces;
using Topiqueue.Core.Exceptions;
using Topiqueue.Core.Messages.Interfaces;
using Topiqueue.Core.Messages.Models;
using Topiqueue.Core.ServiceContainer;

namespace Topiqueue.Core.BackgroundService.Consumers.Services;

internal class HandlerExecutor<T> : IHandlerExecutor where T : ITpqMessageData
{
    private readonly ITpqMessageDataSerializer _serializer;

    public HandlerExecutor(ITpqMessageDataSerializer serializer)
    {
        _serializer = serializer;
    }

    public async Task ExecuteBatchHandler(
        IReadOnlyList<TpqMessageModel> messages,
        ITpqServiceContainerScope serviceContainerScope,
        CancellationToken cancellationToken)
    {
        if (messages.Count == 0)
            return;

        var handler = serviceContainerScope.GetService<T>() as ITpqBatchMessageHandler<T>;
        if (handler == null)
        {
            throw new CreateHandlerException($"Could not create handler for batch of messages with type {messages[0].MessageType}");
        }
        
        var typedMessages = new List<TpqMessageModel<T>>(capacity: messages.Count);
        foreach (var message in messages)
        {
            var typedMessage = new TpqMessageModel<T>
            {
                TopicName = message.TopicName,
                MessageType = message.MessageType,
                PartitionKey = message.PartitionKey,
                PartitionNum = message.PartitionNum,
                CreatedAt = message.CreatedAt,
                Offset = new PartitionOffset
                {
                    TxId = message.TxId,
                    SeqId = message.SeqId,
                    CreatedAt = message.CreatedAt,
                },
                Data = _serializer.DeserializeFromText<T>(message.DataTxt)
            };
            
            typedMessages.Add(typedMessage);
        }
        
        await handler.HandleBatchAsync(typedMessages, cancellationToken);
    }
}