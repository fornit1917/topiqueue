namespace Topiqueue.Core.Dao;

public interface ITpqDao
{
    ITpqDbMigrator Migrator { get; }
    ITpqTopicsDao TopicsDao { get; }
    ITpqServersDao ServersDao { get; }
    ITpqProducerDao ProducerDao { get; }
    ITpqConsumerDao ConsumerDao { get; }
}