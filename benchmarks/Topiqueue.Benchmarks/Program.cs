using System;
using BenchmarkDotNet.Running;
using Topiqueue.Benchmarks.TopiqueueBenchmarks;

namespace Topiqueue.Benchmarks;

public static class Program
{
    static void Main(string[] args)
    {
        var switcher = new BenchmarkSwitcher(typeof(Program).Assembly);
        switcher.Run(args);

        // var b = new TpqHandleBenchmark();
        // b.ParallelismDegree = 10;
        //
        // b.GlobalSetup();
        //
        // Console.WriteLine("WarmUp...");
        // b.IterationSetup();
        // b.TopiqueueHandleMessages();
        // b.IterationCleanup();
        //
        // Console.WriteLine();
        // Console.WriteLine("Run...");
        // b.IterationSetup();
        // b.TopiqueueHandleMessages();
        // b.IterationCleanup();
        //
        // b.GlobalCleanup();
    }
}