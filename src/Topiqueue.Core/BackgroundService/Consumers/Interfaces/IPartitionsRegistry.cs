using System.Collections.Generic;
using Topiqueue.Core.BackgroundService.Consumers.Models;
using Topiqueue.Core.Configuration.Settings;

namespace Topiqueue.Core.BackgroundService.Consumers.Interfaces;

internal interface IPartitionsRegistry
{
    PartitionState Get(TpqConsumerSettings consumer, int partitionNum);
    IEnumerable<PartitionState> GetCaptured();
}