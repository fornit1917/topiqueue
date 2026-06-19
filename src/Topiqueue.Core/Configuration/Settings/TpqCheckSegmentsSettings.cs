using System;
using Topiqueue.Core.Helpers;

namespace Topiqueue.Core.Configuration.Settings;

public class TpqCheckSegmentsSettings
{
    private readonly TimeSpan _checkSegmentsInterval = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _minimumRemainder = TimeSpan.FromMinutes(5);

    public TimeSpan CheckSegmentsInterval
    {
        get => _checkSegmentsInterval; 
        init => _checkSegmentsInterval = value.EnsureGreaterThan(TimeSpan.FromSeconds(1), nameof(CheckSegmentsInterval));
    }

    public TimeSpan MinimumRemainder
    {
        get => _minimumRemainder;
        init => _minimumRemainder = value.EnsureGreaterThan(TimeSpan.FromMinutes(1), nameof(MinimumRemainder));
    }
}