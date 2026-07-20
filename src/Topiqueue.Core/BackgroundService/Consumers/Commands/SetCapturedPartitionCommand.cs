using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Dao.Models;

namespace Topiqueue.Core.BackgroundService.Consumers.Commands;

internal readonly struct SetCapturedPartitionCommand
{
    public required TpqConsumerSettings Consumer { get; init; }
    public required CapturedPartition CapturedPartition { get; init; }
}