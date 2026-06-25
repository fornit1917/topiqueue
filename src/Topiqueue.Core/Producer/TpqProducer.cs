using System.Threading.Tasks;
using Topiqueue.Core.Dao;
using Topiqueue.Core.Messages;
using Topiqueue.Core.Messages.Interfaces;

namespace Topiqueue.Core.Producer;

internal class TpqProducer : ITpqProducer
{
    private readonly ITpqMessageFactory _messageFactory;
    private readonly ITpqProducerDao _producerDao;

    public TpqProducer(ITpqMessageFactory messageFactory, ITpqProducerDao producerDao)
    {
        _messageFactory = messageFactory;
        _producerDao = producerDao;
    }

    public void Produce<T>(string topicName, T data, string? partitionKey = null) where T : ITpqMessageData
    {
        var message = _messageFactory.Create(topicName, data, partitionKey);
        _producerDao.Insert(message);
    }

    public Task ProduceAsync<T>(string topicName, T data, string? partitionKey = null) where T : ITpqMessageData
    {
        var message = _messageFactory.Create(topicName, data, partitionKey);
        return _producerDao.InsertAsync(message);
    }
}