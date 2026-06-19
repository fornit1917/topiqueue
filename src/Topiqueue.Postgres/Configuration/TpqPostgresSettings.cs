namespace Topiqueue.Postgres.Configuration;

public class TpqPostgresSettings
{
    public string Schema { get; init; } = "";
    public string Prefix { get; init; } = "tpq_";
}