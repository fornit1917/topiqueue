using System.Threading.Tasks;
using Topiqueue.Core.Dao;
using Topiqueue.Core.Messages;

namespace Topiqueue.Core.Producer;

internal class TpqProducer : ITpqProducer
{
    private readonly ITpqMessageFactory _messageFactory;
    private readonly ITpqMessagesDao _messagesDao;

    public TpqProducer(ITpqMessageFactory messageFactory, ITpqMessagesDao messagesDao)
    {
        _messageFactory = messageFactory;
        _messagesDao = messagesDao;
    }

    public void Produce<T>(string topicName, T data, string? partitionKey = null) where T : ITpqMessageData
    {
        var message = _messageFactory.Create(topicName, data, partitionKey);
        _messagesDao.Insert(message);
    }

    public Task ProduceAsync<T>(string topicName, T data, string? partitionKey = null) where T : ITpqMessageData
    {
        var message = _messageFactory.Create(topicName, data, partitionKey);
        return _messagesDao.InsertAsync(message);
    }
}