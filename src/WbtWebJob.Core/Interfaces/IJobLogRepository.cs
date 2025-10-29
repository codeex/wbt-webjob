using WbtWebJob.Core.Models;

namespace WbtWebJob.Core.Interfaces;

public interface IJobLogRepository
{
    Task<JobLog> CreateAsync(JobLog log);
    Task<List<JobLog>> GetByJobIdAsync(string jobId);
    Task<JobLog?> GetByIdAsync(string id);
}
