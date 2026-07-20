using System.Threading.Tasks;
using Topiqueue.Core.BackgroundService.Consumers.Commands;

namespace Topiqueue.Core.BackgroundService.Consumers.Interfaces.CommandBus;

internal interface IPartitionsCommandBus
{
    ValueTask Send(CapturePartitionsCommand command);
    ValueTask Send(ReleasePartitionsCommand command);
}