using Topiqueue.Core.Configuration.Settings;

namespace Topiqueue.Core.BackgroundService.Consumers.Models.Commands;

internal struct TopicsReaderCommand
{
    public required TopicsReaderCommandType Type { get; init; }
    public required TpqConsumerSettings Consumer { get; init; }
    public int PartitionNum { get; init; }
    public int PartitionsCount { get; init; }
}