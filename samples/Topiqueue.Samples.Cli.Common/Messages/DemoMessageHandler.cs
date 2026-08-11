using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Topiqueue.Core.Messages.Interfaces;
using Topiqueue.Core.Messages.Models;

namespace Topiqueue.Samples.Cli.Common.Messages;

public class DemoMessageHandler : ITpqBatchMessageHandler<DemoMessageData>
{
    public Task HandleBatchAsync(IReadOnlyList<TpqMessageModel<DemoMessageData>> messages, CancellationToken cancellationToken)
    {
        foreach (var message in messages)
        {
            Console.WriteLine($"Handled message from Topic='{message.TopicName}', Partition={message.PartitionNum}. Id={message.Data?.Id}, Value='{message.Data?.Value}'");
        }
        return Task.CompletedTask;
    }
}