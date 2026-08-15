using Topiqueue.Benchmarks.Common.Topiqueue.Messages;
using Topiqueue.Core.Messages.Interfaces;
using Topiqueue.Core.ServiceContainer;

namespace Topiqueue.Benchmarks.Common.Topiqueue.Services;

public class TpqBenchmarksServicesScope : ITpqServiceContainerScope
{
    public object? GetService<T>()
    {
        var t = typeof(T);
        if (typeof(T) == typeof(ITpqBatchMessageHandler<TpqBenchmarksMessage>))
        {
            return new TpqBenchmarksMessageHandler();
        }
        
        return null;
    }
    
    public void Dispose()
    {
    }
}