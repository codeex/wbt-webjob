using Microsoft.AspNetCore.Mvc;
using WbtWebJob.Models;
using WbtWebJob.Services;

namespace WbtWebJob.Controllers;

/// <summary>
/// MVC Controller for Custom Jobs Web UI
/// </summary>
public class CustomJobsController : Controller
{
    private readonly ICustomJobService _customJobService;
    private readonly IJobService _jobService;
    private readonly IJobExecutor _jobExecutor;
    private readonly ILogger<CustomJobsController> _logger;

    public CustomJobsController(
        ICustomJobService customJobService,
        IJobService jobService,
        IJobExecutor jobExecutor,
        ILogger<CustomJobsController> logger)
    {
        _customJobService = customJobService;
        _jobService = jobService;
        _jobExecutor = jobExecutor;
        _logger = logger;
    }

    // GET: /CustomJobs
    public async Task<IActionResult> Index()
    {
        var customJobs = await _customJobService.GetAllCustomJobsAsync(activeOnly: false);
        return View(customJobs);
    }

    // GET: /CustomJobs/Create
    public IActionResult Create()
    {
        return View(new CustomJob());
    }

    // POST: /CustomJobs/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomJob customJob)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _customJobService.CreateCustomJobAsync(customJob);
                TempData["SuccessMessage"] = "定制任务创建成功！";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建定制任务失败");
                ModelState.AddModelError("", "创建失败: " + ex.Message);
            }
        }

        return View(customJob);
    }

    // GET: /CustomJobs/Edit/{id}
    public async Task<IActionResult> Edit(Guid id)
    {
        var customJob = await _customJobService.GetAllCustomJobsAsync(activeOnly: false);
        var job = customJob.FirstOrDefault(j => j.CustomJobId == id);

        if (job == null)
        {
            return NotFound();
        }

        return View(job);
    }

    // POST: /CustomJobs/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CustomJob customJob)
    {
        if (id != customJob.CustomJobId)
        {
            return BadRequest();
        }

        if (ModelState.IsValid)
        {
            try
            {
                customJob.UpdatedAt = DateTime.UtcNow;
                var success = await _customJobService.UpdateCustomJobAsync(customJob);

                if (success)
                {
                    TempData["SuccessMessage"] = "定制任务更新成功！";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "更新失败，任务未找到");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新定制任务失败");
                ModelState.AddModelError("", "更新失败: " + ex.Message);
            }
        }

        return View(customJob);
    }

    // GET: /CustomJobs/Details/{id}
    public async Task<IActionResult> Details(Guid id)
    {
        var customJobs = await _customJobService.GetAllCustomJobsAsync(activeOnly: false);
        var job = customJobs.FirstOrDefault(j => j.CustomJobId == id);

        if (job == null)
        {
            return NotFound();
        }

        return View(job);
    }

    // POST: /CustomJobs/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var success = await _customJobService.DeleteCustomJobAsync(id);

            if (success)
            {
                TempData["SuccessMessage"] = "定制任务删除成功！";
            }
            else
            {
                TempData["ErrorMessage"] = "删除失败，任务未找到";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除定制任务失败");
            TempData["ErrorMessage"] = "删除失败: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /CustomJobs/Execute/{id}
    [HttpPost]
    public async Task<IActionResult> Execute(Guid id, [FromBody] Dictionary<string, object>? parameters = null)
    {
        try
        {
            var customJobs = await _customJobService.GetAllCustomJobsAsync(activeOnly: false);
            var customJob = customJobs.FirstOrDefault(j => j.CustomJobId == id);

            if (customJob == null)
            {
                return NotFound(new { success = false, message = "任务未找到" });
            }

            // 生成唯一的业务ID
            var businessId = $"{customJob.JobType}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}";

            // 创建任务
            var webJob = await _jobService.CreateJobAsync(
                customJob.JobType,
                businessId,
                $"执行定制任务: {customJob.Name}",
                parameters ?? new Dictionary<string, object>()
            );

            // 执行任务
            await _jobExecutor.ExecuteJobAsync(webJob.JobId);

            return Ok(new
            {
                success = true,
                message = "任务已提交执行",
                jobId = webJob.JobId,
                businessId = businessId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行任务失败");
            return BadRequest(new { success = false, message = "执行失败: " + ex.Message });
        }
    }

    // POST: /CustomJobs/ToggleActive/{id}
    [HttpPost]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        try
        {
            var customJobs = await _customJobService.GetAllCustomJobsAsync(activeOnly: false);
            var customJob = customJobs.FirstOrDefault(j => j.CustomJobId == id);

            if (customJob == null)
            {
                return NotFound(new { success = false, message = "任务未找到" });
            }

            customJob.IsActive = !customJob.IsActive;
            customJob.UpdatedAt = DateTime.UtcNow;

            var success = await _customJobService.UpdateCustomJobAsync(customJob);

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    isActive = customJob.IsActive,
                    message = customJob.IsActive ? "任务已启用" : "任务已禁用"
                });
            }
            else
            {
                return BadRequest(new { success = false, message = "更新失败" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换任务状态失败");
            return BadRequest(new { success = false, message = "操作失败: " + ex.Message });
        }
    }
}
