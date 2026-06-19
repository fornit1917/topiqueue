namespace Topiqueue.Core.Initializer;

public interface ITpqInitializer
{
    void Initialize(bool runDbMigrations = true);
}