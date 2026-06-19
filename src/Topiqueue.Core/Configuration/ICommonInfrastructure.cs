using Microsoft.Extensions.Logging;

namespace Topiqueue.Core.Configuration;

public interface ICommonInfrastructure
{
    public ILoggerFactory LoggerFactory { get; }
}