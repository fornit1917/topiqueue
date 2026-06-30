using System.Threading.Channels;
using System.Threading.Tasks;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces;

namespace Topiqueue.Core.BackgroundService.Consumers.Services;

internal class CommandWriter<T> : ICommandWriter<T>
{
    private readonly ChannelWriter<T> _writer;

    public CommandWriter(ChannelWriter<T> writer)
    {
        _writer = writer;
    }

    public ValueTask Write(T command)
    {
        return _writer.WriteAsync(command);
    }
}