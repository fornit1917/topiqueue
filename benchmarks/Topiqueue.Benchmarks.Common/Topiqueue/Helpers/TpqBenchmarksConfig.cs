using System;
using Npgsql;

namespace Topiqueue.Benchmarks.Common.Topiqueue.Helpers;

public class TpqBenchmarksConfig
{
    public required NpgsqlDataSource DataSource { get; init; }
    
    public string TopicName { get; init; } = "topic_b";
    public int PartitionsCount { get; init; } = 1;
    
    public int DbWorkers { get; init; } = 2;
    public int HandlerWorkers { get; init; } = 1;
    
    public int ReaderBatchSize { get; init; } = 10;
    public int HandlerBatchSize { get; init; } = 1;

    public TimeSpan EmptyTopicPause { get; init; } = TimeSpan.FromMilliseconds(100);
}