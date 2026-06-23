using System;
using System.Threading;
using System.Threading.Tasks;

namespace Topiqueue.Core.Helpers;

public interface ITimerService
{
    Task<bool> TryDelay(TimeSpan timeSpan, CancellationToken cancellationToken = default);
}