using System.Threading;

namespace Topiqueue.Core.BackgroundService.RotateSegments;

public interface IRotateSegmentsService
{
    public void Run(CancellationToken cancellationToken);
}