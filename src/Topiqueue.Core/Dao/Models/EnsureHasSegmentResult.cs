using System;

namespace Topiqueue.Core.Dao.Models;

public class EnsureHasSegmentResult
{
    public DateTime? CreatedSegmentStart { get; init; }
    public DateTime? CreatedSegmentEnd { get; init; }
}