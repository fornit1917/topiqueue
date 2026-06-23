using System;

namespace Topiqueue.Core.Dao.Models;

public class DeletedSegment
{
    public DateTime SegmentStart { get; init; }
    public DateTime SegmentEnd { get; init; }
}