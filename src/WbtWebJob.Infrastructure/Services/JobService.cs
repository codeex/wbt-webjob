using Hangfire;
using WbtWebJob.Core.DTOs;
using WbtWebJob.Core.Enums;
using WbtWebJob.Core.Interfaces;
using WbtWebJob.Core.Models;

namespace WbtWebJob.Infrastructure.Services;

public class JobService : IJobService
{
    private readonly IJobRepository _jobRepository;
    private readonly IJobLogRepository _logRepository;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public JobService(
        IJobRepository jobRepository,
        IJobLogRepository logRepository,
        IBackgroundJobClient backgroundJobClient)
    {
        _jobRepository = jobRepository;
        _logRepository = logRepository;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task<JobResponse> CreateJobAsync(CreateJobRequest request)
    {
        var jobId = GenerateJobId(request.JobType);

        var job = new WebJob
        {
            Id = jobId,
            BusinessId = request.BusinessId,
            JobType = request.JobType,
            Name = request.Name,
            Description = request.Description,
            Configuration = request.Configuration,
            Status = JobStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _jobRepository.CreateAsync(job);

        // 创建Hangfire Job
        var hangfireJobId = _backgroundJobClient.Enqueue(() => ExecuteJobAsync(jobId));
        job.HangfireJobId = hangfireJobId;
        await _jobRepository.UpdateAsync(job);

        return MapToResponse(job);
    }

    public async Task<JobResponse?> GetJobByIdAsync(string jobId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        return job != null ? MapToResponse(job) : null;
    }

    public async Task<JobResponse?> GetJobByBusinessIdAsync(string businessId)
    {
        var job = await _jobRepository.GetByBusinessIdAsync(businessId);
        return job != null ? MapToResponse(job) : null;
    }

    public async Task<List<JobLogResponse>> GetJobLogsAsync(string jobId)
    {
        var logs = await _logRepository.GetByJobIdAsync(jobId);
        return logs.Select(MapToLogResponse).ToList();
    }

    public async Task ExecuteJobAsync(string jobId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null) return;

        try
        {
            job.Status = JobStatus.Running;
            job.StartedAt = DateTime.UtcNow;
            await _jobRepository.UpdateAsync(job);

            await LogAsync(jobId, "Info", "Job started", "开始执行任务");

            // 根据JobType执行不同的逻辑
            switch (job.JobType)
            {
                case JobType.BuiltIn:
                    await ExecuteBuiltInJobAsync(job);
                    break;
                case JobType.Custom:
                    await ExecuteCustomJobAsync(job);
                    break;
                case JobType.Http:
                    await ExecuteHttpJobAsync(job);
                    break;
            }

            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            await LogAsync(jobId, "Info", "Job completed successfully", "任务执行完成");
        }
        catch (Exception ex)
        {
            job.Status = JobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            await LogAsync(jobId, "Error", $"Job failed: {ex.Message}", ex.ToString());
        }
        finally
        {
            job.UpdatedAt = DateTime.UtcNow;
            await _jobRepository.UpdateAsync(job);
        }
    }

    public async Task CancelJobAsync(string jobId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null) return;

        if (!string.IsNullOrEmpty(job.HangfireJobId))
        {
            _backgroundJobClient.Delete(job.HangfireJobId);
        }

        job.Status = JobStatus.Cancelled;
        job.UpdatedAt = DateTime.UtcNow;
        await _jobRepository.UpdateAsync(job);

        await LogAsync(jobId, "Info", "Job cancelled", "任务已取消");
    }

    private async Task ExecuteBuiltInJobAsync(WebJob job)
    {
        await LogAsync(job.Id, "Info", "Executing built-in job", "执行内置任务", "BuiltIn");
        // 内置Job的执行逻辑
        await Task.Delay(1000); // 模拟执行
    }

    private async Task ExecuteCustomJobAsync(WebJob job)
    {
        await LogAsync(job.Id, "Info", "Executing custom job", "执行定制化任务", "Custom");
        // 定制化Job的执行逻辑
        await Task.Delay(1000); // 模拟执行
    }

    private async Task ExecuteHttpJobAsync(WebJob job)
    {
        await LogAsync(job.Id, "Info", "Executing HTTP job", "执行HTTP任务", "HTTP");

        if (string.IsNullOrEmpty(job.Configuration))
        {
            throw new InvalidOperationException("HTTP job configuration is missing");
        }

        // HTTP Job的执行逻辑将在后续实现
        await Task.Delay(1000); // 模拟执行
    }

    private async Task LogAsync(string jobId, string level, string message, string? details = null, string? stepName = null)
    {
        var log = new JobLog
        {
            Id = Guid.NewGuid().ToString(),
            JobId = jobId,
            Level = level,
            Message = message,
            Details = details,
            StepName = stepName,
            CreatedAt = DateTime.UtcNow
        };

        await _logRepository.CreateAsync(log);
    }

    private string GenerateJobId(JobType jobType)
    {
        var prefix = jobType switch
        {
            JobType.BuiltIn => "BI",
            JobType.Custom => "CM",
            JobType.Http => "HTTP",
            _ => "JOB"
        };

        return $"{prefix}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}".Substring(0, 50);
    }

    private JobResponse MapToResponse(WebJob job)
    {
        return new JobResponse
        {
            Id = job.Id,
            BusinessId = job.BusinessId,
            JobType = job.JobType,
            Name = job.Name,
            Description = job.Description,
            Status = job.Status,
            Configuration = job.Configuration,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt
        };
    }

    private JobLogResponse MapToLogResponse(JobLog log)
    {
        return new JobLogResponse
        {
            Id = log.Id,
            JobId = log.JobId,
            Level = log.Level,
            Message = log.Message,
            Details = log.Details,
            StepName = log.StepName,
            CreatedAt = log.CreatedAt
        };
    }
}
