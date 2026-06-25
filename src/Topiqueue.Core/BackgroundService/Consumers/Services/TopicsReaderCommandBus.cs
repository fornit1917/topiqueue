using System.Threading.Channels;
using System.Threading.Tasks;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces;
using Topiqueue.Core.BackgroundService.Consumers.Models;
using Topiqueue.Core.Configuration.Settings;

namespace Topiqueue.Core.BackgroundService.Consumers.Services;

internal class TopicsReaderCommandBus : ITopicsReaderCommandBus
{
    private readonly ChannelWriter<TopicsReaderCommand> _channelWriter;

    public TopicsReaderCommandBus(ChannelWriter<TopicsReaderCommand> channelWriter)
    {
        _channelWriter = channelWriter;
    }

    public ValueTask SendTryCapturePartitionsCommand(TpqConsumerSettings consumer, int partitionCount)
    {
        var command = new TopicsReaderCommand
        {
            Type = TopicsReaderCommandType.TryCapturePartitions,
            Consumer = consumer,
            PartitionsCount = partitionCount
        };
        return _channelWriter.WriteAsync(command);
    }

    public ValueTask SendReleasePartitionsCommand(TpqConsumerSettings consumer, int partitionCount)
    {
        var command = new TopicsReaderCommand
        {
            Type = TopicsReaderCommandType.ReleasePartitions,
            Consumer = consumer,
            PartitionsCount = partitionCount
        };
        return _channelWriter.WriteAsync(command);
    }
}