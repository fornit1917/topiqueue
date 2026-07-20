using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Topiqueue.Core.BackgroundService.Consumers.Commands;
using Topiqueue.Core.BackgroundService.Consumers.Commands.Handlers;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces.CommandBus;
using Topiqueue.Core.Dao.Models;

namespace Topiqueue.Core.BackgroundService.Consumers.Services;

internal class HandlersService : IHandlersService
{
    private readonly Channel<HandleMessagesCommand> _channel;
    private readonly IHandlersResultCommandBus _resultCommandBus;
    private readonly IConsumersContext _consumersContext;

    public HandlersService(
        Channel<HandleMessagesCommand> channel,
        IHandlersResultCommandBus resultCommandBus,
        IConsumersContext consumersContext)
    {
        _channel = channel;
        _resultCommandBus = resultCommandBus;
        _consumersContext = consumersContext;
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
                    await HandleCommand(command, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task HandleCommand(HandleMessagesCommand command, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // todo: handle messages from command
            break;
        }

        var lastMessage = command.MessagesBuffer.Last();
        
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
}