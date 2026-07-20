using System;
using System.Collections.Generic;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Dao.Models;
using Topiqueue.Core.Messages.Models;

namespace Topiqueue.Core.BackgroundService.Consumers.Commands.Dao;

internal readonly struct DaoCommand
{
    public DaoCommandType Type { get; }
    
    private readonly TpqConsumerSettings _consumer;
    private readonly int _partitionsCount;
    private readonly IReadOnlyList<int>? _partitionNums;
    private readonly int _partitionNum;
    private readonly PartitionOffset _offset;
    private readonly List<TpqMessageModel>? _messagesBuffer;

    public DaoCommand(CapturePartitionsDaoCommand command)
    {
        Type = DaoCommandType.CapturePartitions;
        _consumer = command.Consumer;
        _partitionsCount = command.PartitionsCount;
    }

    public DaoCommand(ReleasePartitionsDaoCommand command)
    {
        Type = DaoCommandType.ReleasePartitions;
        _consumer = command.Consumer;
        _partitionNums = command.PartitionsNums;
    }

    public DaoCommand(LoadMessagesDaoCommand command)
    {
        Type = DaoCommandType.LoadMessages;
        _consumer = command.Consumer;
        _partitionNum = command.PartitionNum;
        _offset = command.Offset;
        _messagesBuffer = command.MessagesBuffer;
    }

    public DaoCommand(CommitDaoCommand command)
    {
        Type = DaoCommandType.CommitOffset;
        _consumer = command.Consumer;
        _partitionNum = command.PartitionNum;
        _offset = command.Offset;
    }

    public CapturePartitionsDaoCommand AsCapturePartitions()
    {
        if (Type != DaoCommandType.CapturePartitions)
            throw new InvalidOperationException(
                $"Command with type {Type} cannot be converted to CapturePartitionsDaoCommand");

        return new CapturePartitionsDaoCommand
        {
            Consumer = _consumer,
            PartitionsCount = _partitionsCount,
        };
    }
    
    public ReleasePartitionsDaoCommand AsReleasePartitions()
    {
        if (Type != DaoCommandType.ReleasePartitions)
            throw new InvalidOperationException(
                $"Command with type {Type} cannot be converted to ReleasePartitionsCommand");

        return new ReleasePartitionsDaoCommand
        {
            Consumer = _consumer,
            PartitionsNums = _partitionNums ?? []
        };
    }
    
    public LoadMessagesDaoCommand AsLoadMessages()
    {
        if (Type != DaoCommandType.LoadMessages)
            throw new InvalidOperationException(
                $"Command with type {Type} cannot be converted to LoadMessagesDaoCommand");

        ArgumentNullException.ThrowIfNull(_messagesBuffer);
        
        return new LoadMessagesDaoCommand()
        {
            Consumer = _consumer,
            PartitionNum = _partitionNum,
            Offset = _offset,
            MessagesBuffer = _messagesBuffer,
        };
    }
    
    public CommitDaoCommand AsCommitOffset()
    {
        if (Type != DaoCommandType.CommitOffset)
            throw new InvalidOperationException(
                $"Command with type {Type} cannot be converted to CommitDaoCommand");

        return new CommitDaoCommand
        {
            Consumer = _consumer,
            PartitionNum = _partitionNum,
            Offset = _offset,
        };
    }
}