using System.Collections.Generic;
using Topiqueue.Core.BackgroundService.Consumers.Models.Commands;
using Topiqueue.Core.Configuration;
using Topiqueue.Core.Configuration.Settings;

namespace Topiqueue.Core.BackgroundService.Consumers.Interfaces;

internal interface IConsumersContext
{
    string ServerId { get; }
    ITopicsRegistry Topics { get; }
    IReadOnlyList<TpqConsumerSettings> Consumers { get; }
    TpqBackgroundServiceSettings Settings { get; }
    ICommandWriter<ConsumersCommand> CommandsWriter { get; }
}