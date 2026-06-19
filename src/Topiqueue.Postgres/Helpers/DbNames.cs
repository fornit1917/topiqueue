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

    public static string MessageTable(TpqPostgresSettings settings)
    {
        return For("message", settings);
    }

    public static string TopicTable(TpqPostgresSettings settings)
    {
        return For("topic", settings);
    }

    public static string TopicSegmentTable(TpqPostgresSettings settings)
    {
        return For("topic_segment", settings);
    }

    public static string EnsureTopicCreatedFunction(TpqPostgresSettings settings)
    {
        return For("ensure_topic_created", settings);
    }
    
    public static string CreateTopicSegmentFunction(TpqPostgresSettings settings)
    {
        return For("create_topic_segment", settings);
    }
    
    public static string EnsureTopicHasSegmentFunction(TpqPostgresSettings settings)
    {
        return For("ensure_topic_has_segment", settings);
    }
}