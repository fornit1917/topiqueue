using System.Threading;

namespace Topiqueue.Core.BackgroundService.Consumers.Interfaces;

internal interface IHandlersService
{
    void Run(CancellationToken cancellationToken);
}