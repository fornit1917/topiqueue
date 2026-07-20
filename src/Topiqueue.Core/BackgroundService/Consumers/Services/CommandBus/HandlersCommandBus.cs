using System.Threading.Channels;
using System.Threading.Tasks;
using Topiqueue.Core.BackgroundService.Consumers.Commands.Handlers;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces.CommandBus;

namespace Topiqueue.Core.BackgroundService.Consumers.Services.CommandBus;

internal class HandlersCommandBus : IHandlersCommandBus
{
    private readonly ChannelWriter<HandleMessagesCommand> _channel;

    public HandlersCommandBus(ChannelWriter<HandleMessagesCommand> channel)
    {
        _channel = channel;
    }

    public ValueTask Send(HandleMessagesCommand command)
    {
        return _channel.WriteAsync(command);
    }
}