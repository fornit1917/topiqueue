using Topiqueue.Core.Configuration.Settings;

namespace Topiqueue.Core.BackgroundService.Consumers.Commands.Dao;

internal readonly struct CapturePartitionsDaoCommand
{
    public required TpqConsumerSettings Consumer { get; init; }
    public required int PartitionsCount { get; init; }
}