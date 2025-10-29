using Hangfire;
using Microsoft.AspNetCore.Mvc;
using WbtWebJob.Services;

namespace WbtWebJob.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public JobsController(IJobService jobService, IBackgroundJobClient backgroundJobClient)
    {
        _jobService = jobService;
        _backgroundJobClient = backgroundJobClient;
    }

    [HttpPost]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request)
    {
        if (string.IsNullOrEmpty(request.JobType) || string.IsNullOrEmpty(request.BusinessId))
        {
            return BadRequest(new { error = "JobType and BusinessId are required" });
        }

        var job = await _jobService.CreateJobAsync(
            request.JobType,
            request.BusinessId,
            request.Description,
            request.Parameters);

        // 使用Hangfire调度Job
        var hangfireJobId = _backgroundJobClient.Enqueue<IJobExecutor>(
            executor => executor.ExecuteJobAsync(job.JobId));

        job.HangfireJobId = hangfireJobId;
        await _jobService.UpdateJobStatusAsync(job.JobId, "Queued");

        return Ok(new
        {
            jobId = job.JobId,
            businessId = job.BusinessId,
            status = job.Status,
            hangfireJobId = hangfireJobId,
            createdAt = job.CreatedAt
        });
    }

    [HttpGet("{jobId}")]
    public async Task<IActionResult> GetJob(Guid jobId)
    {
        var job = await _jobService.GetJobByIdAsync(jobId);
        if (job == null)
        {
            return NotFound(new { error = "Job not found" });
        }

        return Ok(job);
    }

    [HttpGet("business/{businessId}")]
    public async Task<IActionResult> GetJobByBusinessId(string businessId)
    {
        var job = await _jobService.GetJobByBusinessIdAsync(businessId);
        if (job == null)
        {
            return NotFound(new { error = "Job not found" });
        }

        return Ok(job);
    }

    [HttpGet("{jobId}/logs")]
    public async Task<IActionResult> GetJobLogs(Guid jobId)
    {
        var logs = await _jobService.GetJobLogsAsync(jobId);
        return Ok(logs);
    }

    [HttpGet("business/{businessId}/logs")]
    public async Task<IActionResult> GetJobLogsByBusinessId(string businessId)
    {
        var logs = await _jobService.GetJobLogsByBusinessIdAsync(businessId);
        return Ok(logs);
    }

    [HttpGet("type/{jobType}")]
    public async Task<IActionResult> GetJobsByType(string jobType)
    {
        var jobs = await _jobService.GetJobsByTypeAsync(jobType);
        return Ok(jobs);
    }
}

public class CreateJobRequest
{
    public string JobType { get; set; } = string.Empty;
    public string BusinessId { get; set; } = string.Empty;
    public string? Description { get; set; }
    public object? Parameters { get; set; }
}
