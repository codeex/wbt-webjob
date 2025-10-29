using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WbtWebJob.Data;
using WbtWebJob.Models;

namespace WbtWebJob.Services;

public class JobService : IJobService
{
    private readonly ApplicationDbContext _context;

    public JobService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WebJob> CreateJobAsync(string jobType, string businessId, string? description, object? parameters)
    {
        var job = new WebJob
        {
            JobId = Guid.NewGuid(),
            JobType = jobType,
            BusinessId = businessId,
            Description = description,
            JobParameters = parameters != null ? JsonSerializer.Serialize(parameters) : null,
            CreatedAt = DateTime.UtcNow,
            Status = "Pending"
        };

        _context.WebJobs.Add(job);
        await _context.SaveChangesAsync();

        return job;
    }

    public async Task<WebJob?> GetJobByIdAsync(Guid jobId)
    {
        return await _context.WebJobs
            .Include(j => j.JobLogs)
            .FirstOrDefaultAsync(j => j.JobId == jobId);
    }

    public async Task<WebJob?> GetJobByBusinessIdAsync(string businessId)
    {
        return await _context.WebJobs
            .Include(j => j.JobLogs)
            .FirstOrDefaultAsync(j => j.BusinessId == businessId);
    }

    public async Task<IEnumerable<WebJob>> GetJobsByTypeAsync(string jobType)
    {
        return await _context.WebJobs
            .Where(j => j.JobType == jobType)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> UpdateJobStatusAsync(Guid jobId, string status, string? errorMessage = null)
    {
        var job = await _context.WebJobs.FindAsync(jobId);
        if (job == null) return false;

        job.Status = status;
        job.ErrorMessage = errorMessage;

        if (status == "Running" && job.StartedAt == null)
        {
            job.StartedAt = DateTime.UtcNow;
        }
        else if (status is "Completed" or "Failed")
        {
            job.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task AddJobLogAsync(Guid jobId, string step, string level, string? message, object? details)
    {
        var log = new JobLog
        {
            LogId = Guid.NewGuid(),
            JobId = jobId,
            Step = step,
            Level = level,
            Message = message,
            Details = details != null ? JsonSerializer.Serialize(details) : null,
            CreatedAt = DateTime.UtcNow
        };

        _context.JobLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<JobLog>> GetJobLogsAsync(Guid jobId)
    {
        return await _context.JobLogs
            .Where(l => l.JobId == jobId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<JobLog>> GetJobLogsByBusinessIdAsync(string businessId)
    {
        var job = await _context.WebJobs
            .FirstOrDefaultAsync(j => j.BusinessId == businessId);

        if (job == null) return Enumerable.Empty<JobLog>();

        return await GetJobLogsAsync(job.JobId);
    }
}
