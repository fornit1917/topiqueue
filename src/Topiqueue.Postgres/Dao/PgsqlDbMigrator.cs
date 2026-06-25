using System.Collections.Generic;
using System.Transactions;
using EvolveDb;
using EvolveDb.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Topiqueue.Core.Dao;
using Topiqueue.Postgres.Configuration;
using Topiqueue.Postgres.Helpers;

namespace Topiqueue.Postgres.Dao;

internal class PgsqlDbMigrator : ITpqDbMigrator
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly TpqPostgresSettings _settings;
    private readonly ILogger<PgsqlDbMigrator> _logger;

    public PgsqlDbMigrator(NpgsqlDataSource dataSource, TpqPostgresSettings settings, ILogger<PgsqlDbMigrator> logger)
    {
        _dataSource = dataSource;
        _settings = settings;
        _logger = logger;
    }

    public void Migrate()
    {
        var conn = _dataSource.OpenConnection();
        var evolve = new Evolve(conn, msg => _logger.LogInformation(msg))
        {
            MetadataTableSchema = _settings.Schema,
            MetadataTableName = $"{_settings.Prefix}evolve_migrations",
            Schemas = string.IsNullOrEmpty(_settings.Schema) ? [] : [_settings.Schema],
            TransactionMode = TransactionKind.CommitAll,
            IsEraseDisabled = true,
            OutOfOrder = true,
            EmbeddedResourceAssemblies = [typeof(PgsqlDbMigrator).Assembly],
            EmbeddedResourceFilters = ["Topiqueue.Postgres.Migrations"],
            Placeholders = new Dictionary<string, string>()
            {
                ["${prefix}"] = _settings.Prefix,
                ["${schema}"] = _settings.Schema.Trim('"'),
                
                ["${message_table}"] = DbNames.MessageTable(_settings),
                ["${topic_table}"] = DbNames.TopicTable(_settings),
                ["${topic_segment_table}"] = DbNames.TopicSegmentTable(_settings),
                ["${topic_consumer_table}"] = DbNames.TopicConsumerTable(_settings),
                ["${server_table}"] = DbNames.ServerTable(_settings),
                ["${server_consumer_table}"] = DbNames.ServerConsumerTable(_settings),
                
                ["${ensure_topic_created_function}"] = DbNames.EnsureTopicCreatedFunction(_settings),
                ["${ensure_topic_has_segment_function}"] = DbNames.EnsureTopicHasSegmentFunction(_settings),
                ["${create_topic_segment_function}"] = DbNames.CreateTopicSegmentFunction(_settings),
                ["${try_delete_outdated_segments_function}"] = DbNames.TryDeleteOutdatedSegmentsFunction(_settings),
            },
        };

        var trOpts = new TransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted
        };
        using var transactionScope = new TransactionScope(
            TransactionScopeOption.Required, 
            trOpts, 
            TransactionScopeAsyncFlowOption.Enabled);

        evolve.Migrate();

        transactionScope.Complete();
    }
}