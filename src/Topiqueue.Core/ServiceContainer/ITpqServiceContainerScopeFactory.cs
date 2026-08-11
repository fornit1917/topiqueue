namespace Topiqueue.Core.ServiceContainer;

public interface ITpqServiceContainerScopeFactory
{
    ITpqServiceContainerScope CreateScope();
}