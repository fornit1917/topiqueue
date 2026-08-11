using System.Collections.Frozen;
using System.Collections.Generic;
using Topiqueue.Core.BackgroundService.Consumers.Interfaces;

namespace Topiqueue.Core.BackgroundService.Consumers.Services;

internal class HandlersRegistry : IHandlersRegistry
{
    private readonly FrozenDictionary<string, IHandlerExecutor> _executors;

    public HandlersRegistry(FrozenDictionary<string, IHandlerExecutor> executors)
    {
        _executors = executors;
    }

    public IHandlerExecutor? GetExecutor(string messageType) => _executors.GetValueOrDefault(messageType);
}