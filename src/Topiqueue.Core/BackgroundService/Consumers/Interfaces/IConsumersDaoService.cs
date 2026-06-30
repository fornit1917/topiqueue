using System.Threading;

namespace Topiqueue.Core.BackgroundService.Consumers.Interfaces;

internal interface IConsumersDaoService
{
    void Run(CancellationToken cancellationToken);
}