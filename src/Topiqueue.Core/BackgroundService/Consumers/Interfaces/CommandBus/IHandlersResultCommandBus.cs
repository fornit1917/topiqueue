using System.Threading.Tasks;
using Topiqueue.Core.BackgroundService.Consumers.Commands;

namespace Topiqueue.Core.BackgroundService.Consumers.Interfaces.CommandBus;

internal interface IHandlersResultCommandBus
{
    ValueTask Send(CommitCommand command);
}