using System;
using Microsoft.Extensions.Logging;

namespace Topiqueue.Core.Logging;

internal class EmptyLoggerFactory : ILoggerFactory
{
    public static EmptyLoggerFactory Instance { get; } = new EmptyLoggerFactory();
    
    public void AddProvider(ILoggerProvider provider)
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new EmptyLogger();
    }

    public void Dispose()
    {
    }
}

internal class EmptyLogger : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return false;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
    }
}