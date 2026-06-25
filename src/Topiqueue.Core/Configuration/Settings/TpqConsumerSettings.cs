using Topiqueue.Core.Helpers;

namespace Topiqueue.Core.Configuration.Settings;

public class TpqConsumerSettings
{
    private readonly string _topicName = "";
    private readonly string _consumerGroupId = "";
    private readonly int _readerBatchSize = 1;
    private readonly int _tryCapturePartitionsOnStart = 1;

    public required string TopicName
    {
        get => _topicName; 
        init => _topicName = value.EnsureNotEmpty(nameof(TopicName));
    }

    public required string ConsumerGroupId
    {
        get => _consumerGroupId; 
        init => _consumerGroupId = value.EnsureNotEmpty(nameof(ConsumerGroupId));
    }

    public int ReaderBatchSize
    {
        get => _readerBatchSize;
        init => _readerBatchSize = value.EnsureGreaterThan(0, nameof(ReaderBatchSize));
    }

    public int TryCapturePartitionsOnStart
    {
        get => _tryCapturePartitionsOnStart;
        init => _tryCapturePartitionsOnStart = value.EnsureGreaterThan(-1, nameof(TryCapturePartitionsOnStart));
    }
}