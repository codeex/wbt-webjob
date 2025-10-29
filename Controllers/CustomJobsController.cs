using Microsoft.AspNetCore.Mvc;
using WbtWebJob.Models;
using WbtWebJob.Services;

namespace WbtWebJob.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomJobsController : ControllerBase
{
    private readonly ICustomJobService _customJobService;

    public CustomJobsController(ICustomJobService customJobService)
    {
        _customJobService = customJobService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomJob([FromBody] CustomJob customJob)
    {
        if (string.IsNullOrEmpty(customJob.JobType) || string.IsNullOrEmpty(customJob.HttpUrl))
        {
            return BadRequest(new { error = "JobType and HttpUrl are required" });
        }

        var created = await _customJobService.CreateCustomJobAsync(customJob);
        return Ok(created);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCustomJobs([FromQuery] bool includeInactive = false)
    {
        var customJobs = await _customJobService.GetAllCustomJobsAsync(!includeInactive);
        return Ok(customJobs);
    }

    [HttpGet("{jobType}")]
    public async Task<IActionResult> GetCustomJobByType(string jobType)
    {
        var customJob = await _customJobService.GetCustomJobByTypeAsync(jobType);
        if (customJob == null)
        {
            return NotFound(new { error = "Custom job not found" });
        }

        return Ok(customJob);
    }

    [HttpPut("{customJobId}")]
    public async Task<IActionResult> UpdateCustomJob(Guid customJobId, [FromBody] CustomJob customJob)
    {
        customJob.CustomJobId = customJobId;
        var updated = await _customJobService.UpdateCustomJobAsync(customJob);

        if (!updated)
        {
            return NotFound(new { error = "Custom job not found" });
        }

        return Ok(new { message = "Custom job updated successfully" });
    }

    [HttpDelete("{customJobId}")]
    public async Task<IActionResult> DeleteCustomJob(Guid customJobId)
    {
        var deleted = await _customJobService.DeleteCustomJobAsync(customJobId);

        if (!deleted)
        {
            return NotFound(new { error = "Custom job not found" });
        }

        return Ok(new { message = "Custom job deleted successfully" });
    }
}
