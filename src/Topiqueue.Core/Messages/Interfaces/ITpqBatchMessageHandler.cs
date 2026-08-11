using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Topiqueue.Core.Messages.Models;

namespace Topiqueue.Core.Messages.Interfaces;

public interface ITpqBatchMessageHandler<T> where T : ITpqMessageData
{
    Task HandleBatchAsync(IReadOnlyList<TpqMessageModel<T>> messages, CancellationToken cancellationToken);
}