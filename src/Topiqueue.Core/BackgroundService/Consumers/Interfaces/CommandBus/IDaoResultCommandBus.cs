using System.Threading.Tasks;
using Topiqueue.Core.BackgroundService.Consumers.Commands;

namespace Topiqueue.Core.BackgroundService.Consumers.Interfaces.CommandBus;

internal interface IDaoResultCommandBus
{
    ValueTask Send(SetCapturedPartitionCommand command);
    ValueTask Send(SetReleasedPartitionCommand command);
    ValueTask Send(SetLoadedMessagesCommand command);
    ValueTask Send(SetCommittedCommand command);
}