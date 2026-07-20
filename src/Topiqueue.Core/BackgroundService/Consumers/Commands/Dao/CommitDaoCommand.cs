using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Dao.Models;

namespace Topiqueue.Core.BackgroundService.Consumers.Commands.Dao;

internal readonly struct CommitDaoCommand
{
    public required TpqConsumerSettings Consumer { get; init; }
    public required int PartitionNum { get; init; }
    public required PartitionOffset Offset { get; init; }
}