using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Topiqueue.Benchmarks.Common.Helpers;
using Topiqueue.Core.Messages.Interfaces;
using Topiqueue.Core.Messages.Models;

namespace Topiqueue.Benchmarks.Common.Topiqueue.Messages;

public class TpqBenchmarksMessage : ITpqMessageData
{
    public static string GetMessageType() => "TpqBenchmarksMessage";
    
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public int DelayMs { get; set; }
}

public class TpqBenchmarksMessageHandler : ITpqBatchMessageHandler<TpqBenchmarksMessage>
{
    public async Task HandleBatchAsync(IReadOnlyList<TpqMessageModel<TpqBenchmarksMessage>> messages, CancellationToken cancellationToken)
    {
        if (messages is not { Count: > 0 })
        {
            return;
        }
        
        if (messages[0].Data.DelayMs > 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(messages[0].Data.DelayMs), cancellationToken);
        }

        foreach (var message in messages)
        {
            Counter.Increment();
        }
    }
}