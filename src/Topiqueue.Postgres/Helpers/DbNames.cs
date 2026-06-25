using Topiqueue.Postgres.Configuration;

namespace Topiqueue.Postgres.Helpers;

internal static class DbNames
{
    public static string For(string name, TpqPostgresSettings settings)
    {
        return string.IsNullOrEmpty(settings.Schema)
            ? $"\"{settings.Prefix}{name}\""
            : $"\"{settings.Schema}\".\"{settings.Prefix}{name}\"";
    }

    public static string MessageTable(TpqPostgresSettings settings) => For("message", settings);

    public static string TopicTable(TpqPostgresSettings settings) => For("topic", settings);

    public static string TopicSegmentTable(TpqPostgresSettings settings) => For("topic_segment", settings);

    public static string ServerTable(TpqPostgresSettings settings) => For("server", settings);

    public static string ServerConsumerTable(TpqPostgresSettings settings) => For("server_consumer", settings);

    public static string TopicConsumerTable(TpqPostgresSettings settings) => For("topic_consumer", settings);

    public static string EnsureTopicCreatedFunction(TpqPostgresSettings settings)
        => For("ensure_topic_created", settings);

    public static string CreateTopicSegmentFunction(TpqPostgresSettings settings)
        => For("create_topic_segment", settings);

    public static string EnsureTopicHasSegmentFunction(TpqPostgresSettings settings)
        => For("ensure_topic_has_segment", settings);

    public static string TryDeleteOutdatedSegmentsFunction(TpqPostgresSettings settings)
        => For("try_delete_outdated_segments", settings);
}