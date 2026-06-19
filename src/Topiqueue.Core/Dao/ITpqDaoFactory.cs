namespace Topiqueue.Core.Dao;

public interface ITpqDaoFactory
{
    ITpqDbMigrator CreateMigrator();
    ITpqTopicsDao CreateTopicsDao();
}