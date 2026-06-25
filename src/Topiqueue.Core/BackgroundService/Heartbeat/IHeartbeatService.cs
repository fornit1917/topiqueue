using System.Threading;

namespace Topiqueue.Core.BackgroundService.Heartbeat;

internal interface IHeartbeatService
{
    public void Run(CancellationToken cancellationToken);
}