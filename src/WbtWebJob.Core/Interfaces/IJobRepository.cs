using WbtWebJob.Core.Models;

namespace WbtWebJob.Core.Interfaces;

public interface IJobRepository
{
    Task<WebJob> CreateAsync(WebJob job);
    Task<WebJob?> GetByIdAsync(string id);
    Task<WebJob?> GetByBusinessIdAsync(string businessId);
    Task<List<WebJob>> GetAllAsync();
    Task UpdateAsync(WebJob job);
    Task DeleteAsync(string id);
}
