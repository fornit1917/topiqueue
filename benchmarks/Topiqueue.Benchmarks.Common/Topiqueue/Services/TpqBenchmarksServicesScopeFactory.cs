using Topiqueue.Core.ServiceContainer;

namespace Topiqueue.Benchmarks.Common.Topiqueue.Services;

public class TpqBenchmarksServicesScopeFactory : ITpqServiceContainerScopeFactory
{
    public ITpqServiceContainerScope CreateScope()
    {
        return new TpqBenchmarksServicesScope();
    }
}