using System.Threading;

namespace Topiqueue.Core.BackgroundService.SegmentsRotation;

internal interface ISegmentsRotationService
{
    public void Run(CancellationToken cancellationToken);
}