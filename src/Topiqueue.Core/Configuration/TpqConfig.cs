using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Dao;
using Topiqueue.Core.Exceptions;
using Topiqueue.Core.Logging;
using Topiqueue.Core.Messages;
using Topiqueue.Core.Messages.Interfaces;
using Topiqueue.Core.Messages.Services;

namespace Topiqueue.Core.Configuration;

public class TpqConfig : ICommonInfrastructure
{
    private readonly List<TpqTopicSettings> _topics = new();
    private readonly List<TpqConsumerSettings> _consumers = new();
    
    private ITpqDao? _dao;
    private Func<ICommonInfrastructure, ITpqDao>? _createDao;
    
    public TpqBackgroundServiceSettings BackgroundServiceSettings { get; private set; } = new();

    public ITpqDao Dao
    {
        get
        {
            _dao ??= _createDao?.Invoke(this);
            return _dao 
                   ?? throw new InvalidTopiqueueConfigException("Dao factory not specified. Call TpqConfig.UseDaoFactory to fix it.");
        }
    }
    
    public ILoggerFactory LoggerFactory { get; private set; } = EmptyLoggerFactory.Instance;

    public ITpqMessageDataSerializer Serializer { get; private set; } =
        new SystemTextJsonSerializer(new JsonSerializerOptions());
    
    public IReadOnlyList<TpqTopicSettings> Topics => _topics;
    
    public IReadOnlyList<TpqConsumerSettings> Consumers => _consumers;

    public TpqConfig UseLoggerFactory(ILoggerFactory loggerFactory)
    {
        LoggerFactory = loggerFactory;
        return this;
    }
    
    public TpqConfig UseDataAccessObjects(ITpqDao dao)
    {
        _dao = dao;
        _createDao = null;
        return this;
    }

    public TpqConfig UseDataAccessObjects(Func<ICommonInfrastructure, ITpqDao> createDao)
    {
        _dao = null;
        _createDao = createDao;
        return this;
    }
    
    public TpqConfig UseTopics(IReadOnlyList<TpqTopicSettings> topics)
    {
        _topics.AddRange(topics);
        return this;
    }

    public TpqConfig UseBackgroundServiceSettings(TpqBackgroundServiceSettings settings)
    {
        BackgroundServiceSettings = settings;
        return this;
    }

    public TpqConfig UseMessageDataSerializer(ITpqMessageDataSerializer serializer)
    {
        Serializer = serializer;
        return this;
    }

    public TpqConfig UseSystemTextJsonMessageDataSerializer(JsonSerializerOptions options)
    {
        return UseMessageDataSerializer(new SystemTextJsonSerializer(options));
    }

    public TpqConfig UseConsumers(IReadOnlyList<TpqConsumerSettings> consumers)
    {
        _consumers.AddRange(consumers);
        return this;
    }
}