using Microsoft.AspNetCore.Mvc;
using WbtWebJob.Models;
using WbtWebJob.Services;
using WbtWebJob.Utils;

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

    // GET: /CustomJobs/CreateWizard
    public IActionResult CreateWizard(int step = 1)
    {
        var model = new CustomJobWizardViewModel
        {
            CurrentStep = step
        };

        // 从TempData恢复数据（如果有）
        if (TempData["WizardData"] is string wizardDataJson)
        {
            try
            {
                var savedModel = System.Text.Json.JsonSerializer.Deserialize<CustomJobWizardViewModel>(wizardDataJson);
                if (savedModel != null)
                {
                    model = savedModel;
                    model.CurrentStep = step;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "恢复向导数据失败");
            }
        }

        return View(model);
    }

    // POST: /CustomJobs/CreateWizard
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateWizard(CustomJobWizardViewModel model, string action)
    {
        // 保存当前数据到TempData
        var wizardDataJson = System.Text.Json.JsonSerializer.Serialize(model);
        TempData["WizardData"] = wizardDataJson;

        if (action == "next")
        {
            // 验证当前步骤
            if (!ValidateWizardStep(model, model.CurrentStep))
            {
                return View(model);
            }

            // 进入下一步
            model.CurrentStep++;
            TempData["WizardData"] = System.Text.Json.JsonSerializer.Serialize(model);
            return RedirectToAction(nameof(CreateWizard), new { step = model.CurrentStep });
        }
        else if (action == "previous")
        {
            // 返回上一步
            model.CurrentStep--;
            return RedirectToAction(nameof(CreateWizard), new { step = model.CurrentStep });
        }
        else if (action == "finish")
        {
            // 验证所有步骤
            if (!ValidateWizardStep(model, 1) || !ValidateWizardStep(model, 3))
            {
                return View(model);
            }

            try
            {
                var customJob = model.ToCustomJob();
                await _customJobService.CreateCustomJobAsync(customJob);

                TempData.Remove("WizardData");
                TempData["SuccessMessage"] = "定制任务创建成功！";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建定制任务失败");
                ModelState.AddModelError("", "创建失败: " + ex.Message);
                return View(model);
            }
        }

        return View(model);
    }

    // POST: /CustomJobs/ParseCurl - AJAX接口，用于解析curl命令
    [HttpPost]
    public IActionResult ParseCurl([FromBody] CurlParseRequest request)
    {
        try
        {
            var parseResult = CurlParser.Parse(request.CurlCommand);

            if (!parseResult.Success)
            {
                return BadRequest(new { success = false, message = parseResult.ErrorMessage });
            }

            return Ok(new
            {
                success = true,
                url = parseResult.Url,
                method = parseResult.Method,
                headers = CurlParser.HeadersToJson(parseResult.Headers),
                requestBody = parseResult.RequestBody
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析curl命令失败");
            return BadRequest(new { success = false, message = "解析失败: " + ex.Message });
        }
    }

    private bool ValidateWizardStep(CustomJobWizardViewModel model, int step)
    {
        switch (step)
        {
            case 1: // 基本信息
                if (string.IsNullOrWhiteSpace(model.JobType))
                {
                    ModelState.AddModelError(nameof(model.JobType), "任务类型不能为空");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    ModelState.AddModelError(nameof(model.Name), "任务名称不能为空");
                    return false;
                }
                break;
            case 3: // HTTP请求
                if (string.IsNullOrWhiteSpace(model.HttpUrl))
                {
                    ModelState.AddModelError(nameof(model.HttpUrl), "HTTP URL不能为空");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(model.HttpMethod))
                {
                    ModelState.AddModelError(nameof(model.HttpMethod), "HTTP方法不能为空");
                    return false;
                }
                break;
        }
        return true;
    }

    public class CurlParseRequest
    {
        public string CurlCommand { get; set; } = string.Empty;
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
            await _jobExecutor.ExecuteJobAsync(webJob.JobId, customJob.JobType, parameters);

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
