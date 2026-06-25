using System.Threading;

namespace Topiqueue.Core.BackgroundService.Consumers.Interfaces;

internal interface ITopicsReaderService
{
    void Run(CancellationToken cancellationToken);
}