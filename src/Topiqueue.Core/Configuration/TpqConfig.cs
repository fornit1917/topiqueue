using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Topiqueue.Core.Configuration.Settings;
using Topiqueue.Core.Dao;
using Topiqueue.Core.Exceptions;
using Topiqueue.Core.Logging;

namespace Topiqueue.Core.Configuration;

public class TpqConfig : ICommonInfrastructure
{
    private readonly List<TpqTopicSettings> _topics = new();
    
    private ITpqDaoFactory? _daoFactory;
    private Func<ICommonInfrastructure, ITpqDaoFactory>? _createDaoFactory;
    
    public TpqCheckSegmentsSettings CheckSegmentsSettings { get; private set; } = new();

    public ITpqDaoFactory DaoFactory
    {
        get
        {
            _daoFactory ??= _createDaoFactory?.Invoke(this);
            return _daoFactory 
                   ?? throw new InvalidTopiqueueConfigException("Dao factory not specified. Call TpqConfig.UseDaoFactory to fix it.");
        }
    }
    
    public ILoggerFactory LoggerFactory { get; private set; } = EmptyLoggerFactory.Instance;
    public IReadOnlyList<TpqTopicSettings> Topics => _topics;

    public TpqConfig UseLoggerFactory(ILoggerFactory loggerFactory)
    {
        LoggerFactory = loggerFactory;
        return this;
    }
    
    public TpqConfig UseDaoFactory(ITpqDaoFactory daoFactory)
    {
        _daoFactory = daoFactory;
        _createDaoFactory = null;
        return this;
    }

    public TpqConfig UseDaoFactory(Func<ICommonInfrastructure, ITpqDaoFactory> createDaoFactory)
    {
        _daoFactory = null;
        _createDaoFactory = createDaoFactory;
        return this;
    }
    
    public TpqConfig UseTopics(IReadOnlyList<TpqTopicSettings> topics)
    {
        _topics.AddRange(topics);
        return this;
    }

    public TpqConfig UseCheckSegmentSettings(TpqCheckSegmentsSettings checkSegmentSettings)
    {
        CheckSegmentsSettings = checkSegmentSettings;
        return this;
    }
}