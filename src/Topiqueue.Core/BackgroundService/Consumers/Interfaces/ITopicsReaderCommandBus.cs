using System.Threading.Tasks;
using Topiqueue.Core.Configuration.Settings;

namespace Topiqueue.Core.BackgroundService.Consumers.Interfaces;

internal interface ITopicsReaderCommandBus
{
    ValueTask SendTryCapturePartitionsCommand(TpqConsumerSettings consumer, int partitionCount);
    ValueTask SendReleasePartitionsCommand(TpqConsumerSettings consumer, int partitionCount);
}