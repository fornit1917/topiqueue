using System;
using System.Threading;
using System.Threading.Tasks;

namespace Topiqueue.Core.Helpers;

internal class TimerService : ITimerService
{
    public static readonly TimerService Instance = new TimerService();
    
    public async Task<bool> TryDelay(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
            return true;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    public async Task RunWithDelay(Func<ValueTask> action, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        if (await TryDelay(delay, cancellationToken))
        {
            await action();
        }
    }
}