using Microsoft.EntityFrameworkCore;
using WbtWebJob.Core.Interfaces;
using WbtWebJob.Core.Models;
using WbtWebJob.Infrastructure.Data;

namespace WbtWebJob.Infrastructure.Repositories;

public class JobLogRepository : IJobLogRepository
{
    private readonly ApplicationDbContext _context;

    public JobLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<JobLog> CreateAsync(JobLog log)
    {
        _context.JobLogs.Add(log);
        await _context.SaveChangesAsync();
        return log;
    }

    public async Task<List<JobLog>> GetByJobIdAsync(string jobId)
    {
        return await _context.JobLogs
            .Where(l => l.JobId == jobId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task<JobLog?> GetByIdAsync(string id)
    {
        return await _context.JobLogs.FindAsync(id);
    }
}
