using System.Threading.Channels;
using System.Threading.Tasks;
using Topiqueue.Core.BackgroundService.Consumers.Commands;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces.CommandBus;

namespace Topiqueue.Core.BackgroundService.Consumers.Services.CommandBus;

internal class DaoResultCommandBus : IDaoResultCommandBus
{
    private readonly ChannelWriter<ConsumersCommand> _channel;

    public DaoResultCommandBus(ChannelWriter<ConsumersCommand> channel)
    {
        _channel = channel;
    }

    public ValueTask Send(SetCapturedPartitionCommand command)
    {
        var setResultCommand = new ConsumersCommand(command);
        return _channel.WriteAsync(setResultCommand);
    }

    public ValueTask Send(SetReleasedPartitionCommand command)
    {
        var setResultCommand = new ConsumersCommand(command);
        return _channel.WriteAsync(setResultCommand);
    }

    public ValueTask Send(SetLoadedMessagesCommand command)
    {
        var setResultCommand = new ConsumersCommand(command);
        return _channel.WriteAsync(setResultCommand);
    }

    public ValueTask Send(SetCommittedCommand command)
    {
        var setResultCommand = new ConsumersCommand(command);
        return _channel.WriteAsync(setResultCommand);
    }
}