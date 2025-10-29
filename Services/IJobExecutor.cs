namespace WbtWebJob.Services;

public interface IJobExecutor
{
    Task ExecuteJobAsync(Guid jobId);
}
