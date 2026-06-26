using System.Threading.Tasks;
using Topiqueue.Core.Configuration.Settings;

namespace Topiqueue.Core.BackgroundService.Consumers.Interfaces;

internal interface IConsumersCommandBus
{
    ValueTask SendTryCapturePartitions(TpqConsumerSettings consumer, int partitionCount);
    ValueTask SendReleasePartitions(TpqConsumerSettings consumer, int partitionCount);
}