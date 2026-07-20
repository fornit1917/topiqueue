using System.Threading.Tasks;
using Topiqueue.Core.BackgroundService.Consumers.Commands.Dao;

namespace Topiqueue.Core.BackgroundService.Consumers.Interfaces.CommandBus;

internal interface IDaoCommandBus
{
    ValueTask Send(CapturePartitionsDaoCommand command);
    ValueTask Send(ReleasePartitionsDaoCommand command);
    ValueTask Send(LoadMessagesDaoCommand command);
    ValueTask Send(CommitDaoCommand command);
}