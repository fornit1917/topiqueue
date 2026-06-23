using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using Topiqueue.Core.Dao;
using Topiqueue.Core.Dao.Models;
using Topiqueue.Core.Exceptions;
using Topiqueue.Postgres.Configuration;
using Topiqueue.Postgres.Helpers;

namespace Topiqueue.Postgres.Dao;

internal class PgsqlTopicsDao : ITpqTopicsDao
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly TpqPostgresSettings _settings;

    private readonly string _ensureTopicCreatedQuery;
    private readonly string _ensureHasSegmentQuery;
    private readonly string _tryDeleteOutdatedSegmentsQuery;

    public PgsqlTopicsDao(NpgsqlDataSource dataSource, TpqPostgresSettings settings)
    {
        _dataSource = dataSource;
        _settings = settings;

        _ensureTopicCreatedQuery = $@"
            SELECT * FROM {DbNames.EnsureTopicCreatedFunction(settings)}($1, $2, $3)
        ";

        _ensureHasSegmentQuery = $@"
            SELECT * FROM {DbNames.EnsureTopicHasSegmentFunction(settings)}($1, $2)
        ";

        _tryDeleteOutdatedSegmentsQuery = $@"
            SELECT * FROM {DbNames.TryDeleteOutdatedSegmentsFunction(settings)}($1, $2)
        ";
    }

    public EnsureTopicCreatedResult EnsureTopicCreated(string topicName, int partitionsCount, TimeSpan retentionInterval)
    {
        using var conn = _dataSource.OpenConnection();

        using var cmd = new NpgsqlCommand(_ensureTopicCreatedQuery, conn);
        cmd.Parameters.Add(new() { Value = topicName });  // 1
        cmd.Parameters.Add(new() { Value = partitionsCount });  // 2
        cmd.Parameters.Add(new() { Value = retentionInterval });   // 3

        using var reader = cmd.ExecuteReader();
        if (!reader.HasRows)
        {
            throw new UnexpectedDbResultException($"Could not receive result of EnsureTopicCreated for topic {topicName}");
        }
        var hasRow = reader.Read();
        if (!hasRow)
        {
            throw new UnexpectedDbResultException($"Could not receive result of EnsureTopicCreated for topic {topicName}");
        }

        var topic = new EnsureTopicCreatedResult
        {
            TopicName = reader.GetString(reader.GetOrdinal("topic_name")),
            TopicSeqId = reader.GetInt32(reader.GetOrdinal("topic_seq_id")),
            PartitionsCount = reader.GetInt32(reader.GetOrdinal("partitions_count")),
            RetentionInterval = reader.GetTimeSpan(reader.GetOrdinal("retention_interval")),
            CreatedNow =  reader.GetBoolean(reader.GetOrdinal("created_now")),
        };
        
        return topic;
    }

    public EnsureHasSegmentResult EnsureTopicHasSegment(string topicName, TimeSpan threshold)
    {
        using var conn = _dataSource.OpenConnection();

        using var cmd = new NpgsqlCommand(_ensureHasSegmentQuery, conn);
        cmd.Parameters.Add(new() { Value = topicName });  // 1
        cmd.Parameters.Add(new() { Value = threshold });  // 2

        using var reader = cmd.ExecuteReader();
        if (!reader.HasRows)
        {
            throw new UnexpectedDbResultException($"Could not receive result of EnsureTopicHasSegment for topic {topicName}");
        }
        var hasRow = reader.Read();
        if (!hasRow)
        {
            throw new UnexpectedDbResultException($"Could not receive result of EnsureTopicHasSegment for topic {topicName}");
        }

        var result = new EnsureHasSegmentResult
        {
            CreatedSegmentStart = reader.GetNullableDatetime("created_segment_start"),
            CreatedSegmentEnd = reader.GetNullableDatetime("created_segment_end"),
        };
        
        return result;
    }

    public async Task<EnsureHasSegmentResult> EnsureTopicHasSegmentAsync(string topicName, TimeSpan threshold)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();

        await using var cmd = new NpgsqlCommand(_ensureHasSegmentQuery, conn);
        cmd.Parameters.Add(new() { Value = topicName });  // 1
        cmd.Parameters.Add(new() { Value = threshold });  // 2

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!reader.HasRows)
        {
            throw new UnexpectedDbResultException($"Could not receive result of EnsureTopicHasSegment for topic {topicName}");
        }
        var hasRow = await reader.ReadAsync();
        if (!hasRow)
        {
            throw new UnexpectedDbResultException($"Could not receive result of EnsureTopicHasSegment for topic {topicName}");
        }

        var result = new EnsureHasSegmentResult
        {
            CreatedSegmentStart = reader.GetNullableDatetime("created_segment_start"),
            CreatedSegmentEnd = reader.GetNullableDatetime("created_segment_end"),
        };
        
        return result;
    }

    public async Task TryDeleteOutdatedSegmentsAsync(string topicName, TimeSpan threshold, List<DeletedSegment> output)
    {
        output.Clear();
        
        await using var conn = await _dataSource.OpenConnectionAsync();

        await using var cmd = new NpgsqlCommand(_tryDeleteOutdatedSegmentsQuery, conn);
        cmd.Parameters.Add(new() { Value = topicName });  // 1
        cmd.Parameters.Add(new() { Value = threshold });  // 2

        await using var reader = await cmd.ExecuteReaderAsync();
        while (true)
        {
            if (!reader.HasRows)
            {
                break;
            }

            var hasRow = await reader.ReadAsync();
            if (!hasRow)
            {
                break;
            }

            var deletedSegment = new DeletedSegment
            {
                SegmentStart = reader.GetDateTime(reader.GetOrdinal("deleted_segment_start")),
                SegmentEnd = reader.GetDateTime(reader.GetOrdinal("deleted_segment_end")),
            };
            output.Add(deletedSegment);
        }
    }
}