using System;
using System.Collections.Generic;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Dao.Models;
using Topiqueue.Core.Messages.Models;

namespace Topiqueue.Core.BackgroundService.Consumers.Commands;

internal readonly struct ConsumersCommand
{
    public ConsumersCommandType Type { get; }

    private readonly TpqConsumerSettings _consumer;
    private readonly int _partitionsCount;
    private readonly CapturedPartition? _capturedPartition;
    private readonly int _partitionNum;
    private readonly PartitionOffset _offset;
    private readonly List<TpqMessageModel>? _messagesBuffer;
    private readonly bool _isStillCaptured;

    public ConsumersCommand(CapturePartitionsCommand command)
    {
        Type = ConsumersCommandType.CapturePartitions;
        _consumer = command.Consumer;
        _partitionsCount = command.PartitionsCount;
    }

    public ConsumersCommand(SetCapturedPartitionCommand command)
    {
        Type = ConsumersCommandType.SetPartitionCaptured;
        _consumer = command.Consumer;
        _capturedPartition = command.CapturedPartition;
    }

    public ConsumersCommand(ReleasePartitionsCommand command)
    {
        Type = ConsumersCommandType.ReleasePartitions;
        _consumer = command.Consumer;
        _partitionsCount = command.PartitionsCount;
    }

    public ConsumersCommand(SetReleasedPartitionCommand command)
    {
        Type = ConsumersCommandType.SetPartitionReleased;
        _consumer = command.Consumer;
        _partitionNum = command.PartitionNum;
    }

    public ConsumersCommand(LoadMessagesCommand command)
    {
        Type = ConsumersCommandType.LoadMessages;
        _consumer = command.Consumer;
        _partitionNum = command.PartitionNum;
        _offset = command.Offset;
    }

    public ConsumersCommand(SetLoadedMessagesCommand command)
    {
        Type = ConsumersCommandType.SetLoadedMessages;
        _consumer = command.Consumer;
        _partitionNum = command.PartitionNum;
        _messagesBuffer = command.MessagesBuffer;
        _offset = command.UsedOffset;
    }

    public ConsumersCommand(CommitCommand command)
    {
        Type = ConsumersCommandType.Commit;
        _consumer = command.Consumer;
        _partitionNum = command.PartitionNum;
        _offset = command.Offset;
        _messagesBuffer = command.HandledMessagesBuffer;
    }

    public ConsumersCommand(SetCommittedCommand command)
    {
        Type = ConsumersCommandType.SetCommitted;
        _consumer = command.Consumer;
        _partitionNum = command.PartitionNum;
        _offset = command.Offset;
        _isStillCaptured = command.IsStillCaptured;
    }

    public CapturePartitionsCommand AsCapturePartitions()
    {
        if (Type != ConsumersCommandType.CapturePartitions)
            throw new InvalidOperationException($"Cannot convert command with type {Type} to CapturePartitionsCommand");

        return new CapturePartitionsCommand
        {
            Consumer = _consumer,
            PartitionsCount = _partitionsCount,
        };
    }

    public SetCapturedPartitionCommand AsSetCapturedPartition()
    {
        if (Type != ConsumersCommandType.SetPartitionCaptured)
            throw new InvalidOperationException($"Cannot convert command with type {Type} to SetPartitionCapturedCommand");
        
        ArgumentNullException.ThrowIfNull(_capturedPartition);

        return new SetCapturedPartitionCommand
        {
            Consumer = _consumer,
            CapturedPartition = _capturedPartition,
        };
    }

    public ReleasePartitionsCommand AsReleasePartitions()
    {
        if (Type != ConsumersCommandType.ReleasePartitions)
            throw new InvalidOperationException($"Cannot convert command with type {Type} to ReleasePartitionsCommand");

        return new ReleasePartitionsCommand
        {
            Consumer = _consumer,
            PartitionsCount = _partitionsCount,
        };
    }
    
    public SetReleasedPartitionCommand AsSetReleasedPartitions()
    {
        if (Type != ConsumersCommandType.SetPartitionReleased)
            throw new InvalidOperationException($"Cannot convert command with type {Type} to SetReleasedPartitionsCommand");

        return new SetReleasedPartitionCommand()
        {
            Consumer = _consumer,
            PartitionNum = _partitionNum,
        };
    }

    public LoadMessagesCommand AsLoadMessages()
    {
        if (Type != ConsumersCommandType.LoadMessages)
            throw new InvalidOperationException($"Cannot convert command with type {Type} to LoadMessagesCommand");

        return new LoadMessagesCommand
        {
            Consumer = _consumer,
            Offset = _offset,
            PartitionNum = _partitionNum,
        };
    }

    public SetLoadedMessagesCommand AsSetLoadedMessages()
    {
        if (Type != ConsumersCommandType.SetLoadedMessages)
            throw new InvalidOperationException($"Cannot convert command with type {Type} to SetLoadedMessagesCommand");
        
        ArgumentNullException.ThrowIfNull(_messagesBuffer);

        return new SetLoadedMessagesCommand
        {
            Consumer = _consumer,
            PartitionNum = _partitionNum,
            MessagesBuffer = _messagesBuffer,
            UsedOffset = _offset,
        };
    }

    public CommitCommand AsCommit()
    {
        if (Type != ConsumersCommandType.Commit)
            throw new InvalidOperationException($"Cannot convert command with type {Type} to CommitCommand");

        ArgumentNullException.ThrowIfNull(_messagesBuffer);
        
        return new CommitCommand
        {
            Consumer = _consumer,
            PartitionNum = _partitionNum,
            Offset = _offset,
            HandledMessagesBuffer = _messagesBuffer,
        };
    }

    public SetCommittedCommand AsSetCommitted()
    {
        if (Type != ConsumersCommandType.SetCommitted)
            throw new InvalidOperationException($"Cannot convert command with type {Type} to SetCommittedCommand");

        return new SetCommittedCommand
        {
            Consumer = _consumer,
            Offset = _offset,
            PartitionNum = _partitionNum,
            IsStillCaptured = _isStillCaptured,
        };
    }
}