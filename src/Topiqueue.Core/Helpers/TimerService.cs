using System;
using System.Threading;
using System.Threading.Tasks;

namespace Topiqueue.Core.Helpers;

internal class TimerService : ITimerService
{
    public static readonly TimerService Instance = new TimerService();
    
    public async Task<bool> TryDelay(TimeSpan timeSpan, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(timeSpan, cancellationToken);
            return true;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }
}