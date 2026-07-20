using System.Collections.Generic;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Dao.Models;
using Topiqueue.Core.Messages.Models;

namespace Topiqueue.Core.BackgroundService.Consumers.Commands.Dao;

internal readonly struct LoadMessagesDaoCommand
{
    public required TpqConsumerSettings Consumer { get; init; }
    public required int PartitionNum { get; init; }
    public required PartitionOffset Offset { get; init; }
    public required List<TpqMessageModel> MessagesBuffer {  get; init; }
}