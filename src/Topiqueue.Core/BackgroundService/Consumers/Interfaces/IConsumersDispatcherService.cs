using System.Threading;

namespace Topiqueue.Core.BackgroundService.Consumers.Interfaces;

internal interface IConsumersDispatcherService
{
    void Run(CancellationToken cancellationToken);
}