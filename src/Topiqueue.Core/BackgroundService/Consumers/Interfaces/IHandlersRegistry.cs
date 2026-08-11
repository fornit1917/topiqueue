namespace Topiqueue.Core.BackgroundService.Consumers.Interfaces;

internal interface IHandlersRegistry
{
    IHandlerExecutor? GetExecutor(string messageType);
}