using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Topiqueue.Core.BackgroundService.Consumers.Commands;
using Topiqueue.Core.BackgroundService.Consumers.Commands.Handlers;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces.CommandBus;
using Topiqueue.Core.Exceptions;
using Topiqueue.Core.Helpers;
using Topiqueue.Core.Messages.Models;
using Topiqueue.Core.ServiceContainer;

namespace Topiqueue.Core.BackgroundService.Consumers.Services;

internal class HandlersService : IHandlersService
{
    private readonly Channel<HandleMessagesCommand> _channel;
    private readonly ITpqServiceContainerScopeFactory _serviceContainerScopeFactory;
    private readonly IHandlersRegistry _handlersRegistry;
    private readonly IHandlersResultCommandBus _resultCommandBus;
    private readonly ITimerService _timerService;
    private readonly IConsumersContext _consumersContext;
    private readonly ILogger<HandlersService> _logger;

    public HandlersService(
        Channel<HandleMessagesCommand> channel,
        ITpqServiceContainerScopeFactory serviceContainerScopeFactory,
        IHandlersRegistry handlersRegistry,
        IHandlersResultCommandBus resultCommandBus,
        ITimerService timerService,
        IConsumersContext consumersContext,
        ILogger<HandlersService> logger)
    {
        _channel = channel;
        _serviceContainerScopeFactory = serviceContainerScopeFactory;
        _handlersRegistry = handlersRegistry;
        _resultCommandBus = resultCommandBus;
        _timerService = timerService;
        _consumersContext = consumersContext;
        _logger = logger;
    }

    public void Run(CancellationToken cancellationToken)
    {
        for (int i = 0; i < _consumersContext.Settings.MessagesHandlerWorkers; i++)
        {
            _ = Task.Run(async () => await HandleCommandsProcess(cancellationToken), cancellationToken);
        }
    }

    private async Task HandleCommandsProcess(CancellationToken cancellationToken)
    {
        var internalMessagesBuffer = new List<TpqMessageModel>();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var channelOpen = await _channel.Reader.WaitToReadAsync(cancellationToken);
                if (!channelOpen)
                {
                    return;
                }

                while (!cancellationToken.IsCancellationRequested && _channel.Reader.TryRead(out var command))
                {
                    await HandleCommand(command, internalMessagesBuffer, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task HandleCommand(HandleMessagesCommand command, List<TpqMessageModel> internalMessageBuffer, 
        CancellationToken cancellationToken)
    {
        var messages = command.MessagesBuffer;
        internalMessageBuffer.Clear();
        internalMessageBuffer.Add(messages[0]);
        int i = 1;
        while (i < messages.Count)
        {
            if (messages[i].MessageType == internalMessageBuffer[0].MessageType)
            {
                internalMessageBuffer.Add(messages[i]);
            }
            else
            {
                await HandleMessages(internalMessageBuffer, cancellationToken);
                internalMessageBuffer.Clear();
                internalMessageBuffer.Add(messages[i]);
            }
            
            ++i;
        }

        if (internalMessageBuffer.Count > 0)
        {
            await HandleMessages(internalMessageBuffer, cancellationToken);
            internalMessageBuffer.Clear();
        }

        var lastMessage = messages.Last();
        
        var offset = new PartitionOffset
        {
            TxId = lastMessage.TxId,
            SeqId = lastMessage.SeqId,
            CreatedAt = lastMessage.CreatedAt,
        };

        var commitCommand = new CommitCommand
        {
            Consumer = command.Consumer,
            PartitionNum = command.PartitionNum,
            Offset = offset,
            HandledMessagesBuffer = command.MessagesBuffer
        };
        
        await _resultCommandBus.Send(commitCommand);
    }

    private async Task HandleMessages(IReadOnlyList<TpqMessageModel> messages, CancellationToken cancellationToken)
    {
        if (messages.Count == 0)
            return;
        
        var messageType = messages[0].MessageType;

        var handled = false;
        while (!cancellationToken.IsCancellationRequested && !handled)
        {
            using var serviceContainerScope = _serviceContainerScopeFactory.CreateScope();
            
            try
            {
                var handlerExecutor = _handlersRegistry.GetExecutor(messageType);
                if (handlerExecutor is null)
                {
                    throw new UnknownMessageTypeException($"Handler is not specified for message type: {messageType}");
                }
        
                await handlerExecutor.ExecuteBatchHandler(messages, serviceContainerScope, cancellationToken);
                handled = true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error processing messages from {TopicName}.{PartitionNum} with type {MessageType}, offset {TxId}.{SeqId}, batch size {BatchSize}",
                    messages[0].TopicName, messages[0].PartitionNum, messages[0].MessageType, 
                    messages[0].TxId, messages[0].SeqId, messages.Count);
                
                // todo: Use retry policy
                await _timerService.TryDelay(TimeSpan.FromSeconds(1), cancellationToken);
            }            
        }
    }
}