using System.Threading;

namespace Topiqueue.Core.BackgroundService.Consumers.Interfaces;

internal interface IPartitionsBalancerService
{
    void Run(CancellationToken cancellationToken);
}