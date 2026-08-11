using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Topiqueue.Core.Messages.Interfaces;
using Topiqueue.Core.Messages.Models;
using Topiqueue.Core.ServiceContainer;

namespace Topiqueue.Core.BackgroundService.Consumers.Interfaces;

internal interface IHandlerExecutor
{
    Task ExecuteBatchHandler(
        IReadOnlyList<TpqMessageModel> messages,
        ITpqServiceContainerScope serviceContainerScope,
        CancellationToken cancellationToken);
}