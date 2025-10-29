using Microsoft.EntityFrameworkCore;
using WbtWebJob.Core.Interfaces;
using WbtWebJob.Core.Models;
using WbtWebJob.Infrastructure.Data;

namespace WbtWebJob.Infrastructure.Repositories;

public class JobRepository : IJobRepository
{
    private readonly ApplicationDbContext _context;

    public JobRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WebJob> CreateAsync(WebJob job)
    {
        _context.WebJobs.Add(job);
        await _context.SaveChangesAsync();
        return job;
    }

    public async Task<WebJob?> GetByIdAsync(string id)
    {
        return await _context.WebJobs
            .Include(j => j.Logs)
            .FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task<WebJob?> GetByBusinessIdAsync(string businessId)
    {
        return await _context.WebJobs
            .Include(j => j.Logs)
            .FirstOrDefaultAsync(j => j.BusinessId == businessId);
    }

    public async Task<List<WebJob>> GetAllAsync()
    {
        return await _context.WebJobs
            .Include(j => j.Logs)
            .ToListAsync();
    }

    public async Task UpdateAsync(WebJob job)
    {
        _context.WebJobs.Update(job);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var job = await GetByIdAsync(id);
        if (job != null)
        {
            _context.WebJobs.Remove(job);
            await _context.SaveChangesAsync();
        }
    }
}
