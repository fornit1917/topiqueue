using Topiqueue.Core.ServiceContainer;

namespace Topiqueue.Samples.Cli.Common.ServiceContainer;

public class SampleCliServiceContainerScopeFactory : ITpqServiceContainerScopeFactory
{
    public ITpqServiceContainerScope CreateScope()
    {
        return new SampleCliServiceContainerScope();
    }
}