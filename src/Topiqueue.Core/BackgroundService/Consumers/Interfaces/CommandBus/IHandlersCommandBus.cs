using System.Threading.Tasks;
using Topiqueue.Core.BackgroundService.Consumers.Commands.Handlers;

namespace Topiqueue.Core.BackgroundService.Consumers.Interfaces.CommandBus;

internal interface IHandlersCommandBus
{
    ValueTask Send(HandleMessagesCommand command);
}