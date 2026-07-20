using System.Threading.Channels;
using System.Threading.Tasks;
using Topiqueue.Core.BackgroundService.Consumers.Commands;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces.CommandBus;

namespace Topiqueue.Core.BackgroundService.Consumers.Services.CommandBus;

internal class HandlersResultCommandBus : IHandlersResultCommandBus
{
    private readonly ChannelWriter<ConsumersCommand> _channel;

    public HandlersResultCommandBus(ChannelWriter<ConsumersCommand> channel)
    {
        _channel = channel;
    }

    public ValueTask Send(CommitCommand command)
    {
        var setResultCommand = new ConsumersCommand(command);
        return _channel.WriteAsync(setResultCommand);
    }
}