using System.Threading.Tasks;
using Jobby.Core.Interfaces;
using Jobby.Core.Models;
using Topiqueue.Benchmarks.Common.Helpers;

namespace Topiqueue.Benchmarks.JobbyBenchmarks.Commands;

public class JobbyBenchmarksCommand : IJobCommand
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public int DelayMs { get; set; }

    public static string GetJobName() => "JobbyBenchmarks";
}

public class JobbyBenchmarksCommandHandler : IJobCommandHandler<JobbyBenchmarksCommand>
{
    public async Task ExecuteAsync(JobbyBenchmarksCommand command, JobExecutionContext ctx)
    {
        if (command?.DelayMs > 0)
        {
            await Task.Delay(command.DelayMs);
        }
        
        Counter.Increment();
    }
}