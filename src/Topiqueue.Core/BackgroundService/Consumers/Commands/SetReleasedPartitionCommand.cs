using Topiqueue.Core.Configuration.Settings;

namespace Topiqueue.Core.BackgroundService.Consumers.Commands;

internal readonly struct SetReleasedPartitionCommand
{
    public required TpqConsumerSettings Consumer { get; init; }
    public required int PartitionNum { get; init; }
}