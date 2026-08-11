using System;
using Topiqueue.Core.ServiceContainer;
using Topiqueue.Samples.Cli.Common.Messages;

namespace Topiqueue.Samples.Cli.Common.ServiceContainer;

public class SampleCliServiceContainerScope : ITpqServiceContainerScope
{
    public object? GetService<T>()
    {
        if (typeof(T) == typeof(DemoMessageData))
        {
            return new DemoMessageHandler();
        }

        throw new Exception($"Not registered service type: {typeof(T)}");
    }
    
    public void Dispose()
    {
    }
}