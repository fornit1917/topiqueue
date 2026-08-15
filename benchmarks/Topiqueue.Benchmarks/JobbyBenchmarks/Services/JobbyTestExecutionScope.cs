using System;
using Jobby.Core.Interfaces;
using Topiqueue.Benchmarks.JobbyBenchmarks.Commands;

namespace Topiqueue.Benchmarks.JobbyBenchmarks.Services;

public class JobbyTestExecutionScope : IJobExecutionScope
{
    public void Dispose()
    {
    }

    public object? GetService(Type type)
    {
        if (type == typeof(IJobCommandHandler<JobbyBenchmarksCommand>))
        {
            return new JobbyBenchmarksCommandHandler();
        }
        return null;
    }
}