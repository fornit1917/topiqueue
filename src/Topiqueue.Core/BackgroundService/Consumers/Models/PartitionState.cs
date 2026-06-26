using System.Threading;
using Topiqueue.Core.Configuration.Settings;

namespace Topiqueue.Core.BackgroundService.Consumers.Models;

internal class PartitionState
{
    private int _captured;

    public bool TrySetCaptured()
    {
        var prev = Interlocked.CompareExchange(ref _captured, 1, 0);
        return prev == 0;
    }

    public bool TrySetReleased()
    {
        var prev = Interlocked.CompareExchange(ref _captured, 0, 1);
        return prev == 1;
    }
}