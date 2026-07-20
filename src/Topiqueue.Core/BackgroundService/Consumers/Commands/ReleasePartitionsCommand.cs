using Topiqueue.Core.Configuration.Settings;

namespace Topiqueue.Core.BackgroundService.Consumers.Commands;

internal readonly struct ReleasePartitionsCommand
{
    public required TpqConsumerSettings Consumer { get; init; }
    public required int PartitionsCount { get; init; }
}