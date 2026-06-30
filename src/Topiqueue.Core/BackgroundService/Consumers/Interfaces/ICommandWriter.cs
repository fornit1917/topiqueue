using System.Threading.Tasks;

namespace Topiqueue.Core.BackgroundService.Consumers.Interfaces;

internal interface ICommandWriter<in T>
{
    ValueTask Write(T command);
}