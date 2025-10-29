using Microsoft.AspNetCore.Mvc;
using WbtWebJob.Core.DTOs;
using WbtWebJob.Core.Interfaces;

namespace WbtWebJob.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(IJobService jobService, ILogger<JobsController> logger)
    {
        _jobService = jobService;
        _logger = logger;
    }

    /// <summary>
    /// 创建WebJob
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<JobResponse>> CreateJob([FromBody] CreateJobRequest request)
    {
        try
        {
            var job = await _jobService.CreateJobAsync(request);
            return CreatedAtAction(nameof(GetJobById), new { id = job.Id }, job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job");
            return StatusCode(500, new { error = "Failed to create job", message = ex.Message });
        }
    }

    /// <summary>
    /// 根据JobId获取Job信息
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<JobResponse>> GetJobById(string id)
    {
        try
        {
            var job = await _jobService.GetJobByIdAsync(id);
            if (job == null)
            {
                return NotFound(new { error = "Job not found", jobId = id });
            }
            return Ok(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job by id: {JobId}", id);
            return StatusCode(500, new { error = "Failed to get job", message = ex.Message });
        }
    }

    /// <summary>
    /// 根据业务ID获取Job信息
    /// </summary>
    [HttpGet("business/{businessId}")]
    public async Task<ActionResult<JobResponse>> GetJobByBusinessId(string businessId)
    {
        try
        {
            var job = await _jobService.GetJobByBusinessIdAsync(businessId);
            if (job == null)
            {
                return NotFound(new { error = "Job not found", businessId });
            }
            return Ok(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job by business id: {BusinessId}", businessId);
            return StatusCode(500, new { error = "Failed to get job", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取Job执行日志
    /// </summary>
    [HttpGet("{id}/logs")]
    public async Task<ActionResult<List<JobLogResponse>>> GetJobLogs(string id)
    {
        try
        {
            var logs = await _jobService.GetJobLogsAsync(id);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job logs: {JobId}", id);
            return StatusCode(500, new { error = "Failed to get job logs", message = ex.Message });
        }
    }

    /// <summary>
    /// 取消Job执行
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<ActionResult> CancelJob(string id)
    {
        try
        {
            await _jobService.CancelJobAsync(id);
            return Ok(new { message = "Job cancelled successfully", jobId = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling job: {JobId}", id);
            return StatusCode(500, new { error = "Failed to cancel job", message = ex.Message });
        }
    }
}
