using Topiqueue.Core.Configuration.Settings;

namespace Topiqueue.Core.BackgroundService.Consumers.Commands;

internal readonly struct CapturePartitionsCommand
{
    public required TpqConsumerSettings Consumer { get; init; }
    public required int PartitionsCount { get; init; }
}