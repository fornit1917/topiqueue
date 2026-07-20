using System.Collections.Generic;
using Topiqueue.Core.Configuration.Settings;

namespace Topiqueue.Core.BackgroundService.Consumers.Commands.Dao;

internal readonly struct ReleasePartitionsDaoCommand
{
    public required TpqConsumerSettings Consumer { get; init; }
    public required IReadOnlyList<int> PartitionsNums { get; init; }
}