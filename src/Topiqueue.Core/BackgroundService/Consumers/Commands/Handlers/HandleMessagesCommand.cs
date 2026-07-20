using System.Collections.Generic;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Messages.Models;

namespace Topiqueue.Core.BackgroundService.Consumers.Commands.Handlers;

internal readonly struct HandleMessagesCommand
{
    public required TpqConsumerSettings Consumer { get; init; }
    public required int PartitionNum { get; init; }
    public required List<TpqMessageModel> MessagesBuffer { get; init; }
}