using System;
using System.Threading;
using System.Threading.Tasks;

namespace Topiqueue.Core.Helpers;

public interface ITimerService
{
    Task<bool> TryDelay(TimeSpan delay, CancellationToken cancellationToken = default);
    Task RunWithDelay(Func<ValueTask> action, TimeSpan delay, CancellationToken cancellationToken = default);
}