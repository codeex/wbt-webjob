using Microsoft.EntityFrameworkCore;
using WbtWebJob.Data;
using WbtWebJob.Models;

namespace WbtWebJob.Services;

public class CustomJobService : ICustomJobService
{
    private readonly ApplicationDbContext _context;

    public CustomJobService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CustomJob> CreateCustomJobAsync(CustomJob customJob)
    {
        customJob.CustomJobId = Guid.NewGuid();
        customJob.CreatedAt = DateTime.UtcNow;

        _context.CustomJobs.Add(customJob);
        await _context.SaveChangesAsync();

        return customJob;
    }

    public async Task<CustomJob?> GetCustomJobByTypeAsync(string jobType)
    {
        return await _context.CustomJobs
            .FirstOrDefaultAsync(c => c.JobType == jobType && c.IsActive);
    }

    public async Task<IEnumerable<CustomJob>> GetAllCustomJobsAsync(bool activeOnly = true)
    {
        var query = _context.CustomJobs.AsQueryable();

        if (activeOnly)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<bool> UpdateCustomJobAsync(CustomJob customJob)
    {
        var existing = await _context.CustomJobs.FindAsync(customJob.CustomJobId);
        if (existing == null) return false;

        existing.Name = customJob.Name;
        existing.Description = customJob.Description;
        existing.HttpUrl = customJob.HttpUrl;
        existing.HttpMethod = customJob.HttpMethod;
        existing.AuthType = customJob.AuthType;
        existing.AuthConfig = customJob.AuthConfig;
        existing.Headers = customJob.Headers;
        existing.DefaultParameters = customJob.DefaultParameters;
        existing.IsActive = customJob.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCustomJobAsync(Guid customJobId)
    {
        var customJob = await _context.CustomJobs.FindAsync(customJobId);
        if (customJob == null) return false;

        _context.CustomJobs.Remove(customJob);
        await _context.SaveChangesAsync();
        return true;
    }
}
