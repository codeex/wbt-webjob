using WbtWebJob.Models;

namespace WbtWebJob.Services;

public interface IJobService
{
    Task<WebJob> CreateJobAsync(string jobType, string businessId, string? description, object? parameters);
    Task<WebJob?> GetJobByIdAsync(Guid jobId);
    Task<WebJob?> GetJobByBusinessIdAsync(string businessId);
    Task<IEnumerable<WebJob>> GetJobsByTypeAsync(string jobType);
    Task<bool> UpdateJobStatusAsync(Guid jobId, string status, string? errorMessage = null);
    Task AddJobLogAsync(Guid jobId, string step, string level, string? message, object? details);
    Task<IEnumerable<JobLog>> GetJobLogsAsync(Guid jobId);
    Task<IEnumerable<JobLog>> GetJobLogsByBusinessIdAsync(string businessId);
}
