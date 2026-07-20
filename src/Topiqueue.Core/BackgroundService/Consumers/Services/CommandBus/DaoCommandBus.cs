using System.Threading.Channels;
using System.Threading.Tasks;
using Topiqueue.Core.BackgroundService.Consumers.Commands.Dao;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces.CommandBus;

namespace Topiqueue.Core.BackgroundService.Consumers.Services.CommandBus;

internal class DaoCommandBus : IDaoCommandBus
{
    private readonly ChannelWriter<DaoCommand> _channel;

    public DaoCommandBus(ChannelWriter<DaoCommand> channel)
    {
        _channel = channel;
    }

    public ValueTask Send(CapturePartitionsDaoCommand command)
    {
        var daoCommand = new DaoCommand(command);
        return _channel.WriteAsync(daoCommand);
    }

    public ValueTask Send(ReleasePartitionsDaoCommand command)
    {
        var daoCommand = new DaoCommand(command);
        return _channel.WriteAsync(daoCommand);
    }

    public ValueTask Send(LoadMessagesDaoCommand command)
    {
        var daoCommand = new DaoCommand(command);
        return _channel.WriteAsync(daoCommand);
    }

    public ValueTask Send(CommitDaoCommand command)
    {
        var daoCommand = new DaoCommand(command);
        return _channel.WriteAsync(daoCommand);
    }
}