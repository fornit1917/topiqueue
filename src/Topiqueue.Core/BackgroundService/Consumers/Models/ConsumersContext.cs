using System.Collections.Generic;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces;
using Topiqueue.Core.BackgroundService.Consumers.Models.Commands;
using Topiqueue.Core.Configuration;
using Topiqueue.Core.Configuration.Settings;

namespace Topiqueue.Core.BackgroundService.Consumers.Models;

internal class ConsumersContext : IConsumersContext
{
    public required string ServerId { get; init; }
    public required ITopicsRegistry Topics { get; init; }
    public required IReadOnlyList<TpqConsumerSettings> Consumers { get; init; }
    public required TpqBackgroundServiceSettings Settings { get; init; }
    public required ICommandWriter<ConsumersCommand> CommandsWriter { get; init; }
}