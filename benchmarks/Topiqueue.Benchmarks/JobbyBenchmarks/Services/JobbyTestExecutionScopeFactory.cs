using Jobby.Core.Interfaces;

namespace Topiqueue.Benchmarks.JobbyBenchmarks.Services;

public class JobbyTestExecutionScopeFactory : IJobExecutionScopeFactory
{
    public IJobExecutionScope CreateJobExecutionScope()
    {
        return new JobbyTestExecutionScope();
    }
}