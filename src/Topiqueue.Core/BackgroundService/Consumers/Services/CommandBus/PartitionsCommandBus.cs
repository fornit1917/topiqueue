using System.Threading.Channels;
using System.Threading.Tasks;
using Topiqueue.Core.BackgroundService.Consumers.Commands;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces.CommandBus;

namespace Topiqueue.Core.BackgroundService.Consumers.Services.CommandBus;

internal class PartitionsCommandBus : IPartitionsCommandBus
{
    private readonly ChannelWriter<ConsumersCommand> _channel;

    public PartitionsCommandBus(ChannelWriter<ConsumersCommand> channel)
    {
        _channel = channel;
    }

    public ValueTask Send(CapturePartitionsCommand command)
    {
        var partitionsCommand = new ConsumersCommand(command);
        return _channel.WriteAsync(partitionsCommand);
    }

    public ValueTask Send(ReleasePartitionsCommand command)
    {
        var partitionsCommand = new ConsumersCommand(command);
        return _channel.WriteAsync(partitionsCommand);
    }
}