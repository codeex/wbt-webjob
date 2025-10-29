using WbtWebJob.Models;

namespace WbtWebJob.Services;

public interface ICustomJobService
{
    Task<CustomJob> CreateCustomJobAsync(CustomJob customJob);
    Task<CustomJob?> GetCustomJobByTypeAsync(string jobType);
    Task<IEnumerable<CustomJob>> GetAllCustomJobsAsync(bool activeOnly = true);
    Task<bool> UpdateCustomJobAsync(CustomJob customJob);
    Task<bool> DeleteCustomJobAsync(Guid customJobId);
}
