using System;
using Topiqueue.Core.Messages.Interfaces;

namespace Topiqueue.Core.ServiceContainer;

public interface ITpqServiceContainerScope : IDisposable
{
    object? GetService<T>();
}