using System;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;

namespace Topiqueue.Benchmarks.Common.Helpers;

public static class Counter
{
    public static int Value = 0;
    public static int CompletedValue = 1000;
    public static Task Task => Tcs.Task;
    
    private static TaskCompletionSource Tcs = new TaskCompletionSource();
    private static long StartTs = 0;
    private static long StopTs = 0;

    public static void Increment()
    {
        var newValue = Interlocked.Increment(ref Value);
        if (newValue == CompletedValue)
        {
            StopTs = Stopwatch.GetTimestamp();
            Tcs.SetResult();
        }
    }

    public static void Reset(int completedValue)
    {
        Tcs = new TaskCompletionSource();
        Value = 0;
        CompletedValue = completedValue;
    }

    public static void StartTimer()
    {
        StartTs = Stopwatch.GetTimestamp();
        StopTs = 0;
    }
    
    public static TimeSpan GetElapsedTime()
    {
        return Stopwatch.GetElapsedTime(StartTs, StopTs);
    }
}