using Hangfire;
using System.Text;
using System.Text.Json;

namespace WbtWebJob.Services;

/// <summary>
/// Hangfire作业服务实现
/// </summary>
public class HangfireJobService : IHangfireJobService
{
    private readonly ICustomJobService _customJobService;
    private readonly IJobService _jobService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HangfireJobService> _logger;

    public HangfireJobService(
        ICustomJobService customJobService,
        IJobService jobService,
        IHttpClientFactory httpClientFactory,
        ILogger<HangfireJobService> logger)
    {
        _customJobService = customJobService;
        _jobService = jobService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// 添加一次性测试任务到Hangfire
    /// </summary>
    public string EnqueueTestJob(Guid customJobId, Dictionary<string, object>? parameters = null)
    {
        var jobId = BackgroundJob.Enqueue(() => ExecuteCustomJobAsync(customJobId, parameters));
        _logger.LogInformation($"Enqueued test job for CustomJob {customJobId}, Hangfire JobId: {jobId}");
        return jobId;
    }

    /// <summary>
    /// 添加定时任务到Hangfire
    /// </summary>
    public async Task<string> ScheduleRecurringJob(Guid customJobId, string cronExpression, Dictionary<string, object>? parameters = null)
    {
        // 获取CustomJob信息以便设置显示名称
        var customJobs = await _customJobService.GetAllCustomJobsAsync(activeOnly: false);
        var customJob = customJobs.FirstOrDefault(j => j.CustomJobId == customJobId);

        if (customJob == null)
        {
            _logger.LogError($"CustomJob {customJobId} not found");
            throw new Exception($"CustomJob {customJobId} not found");
        }

        var recurringJobId = $"CustomJob_{customJobId}";

        // 设置显示名称：任务名称 + 任务ID
        var displayName = $"{customJob.Name} (ID: {customJobId})";

        RecurringJob.AddOrUpdate(
            recurringJobId,
            () => ExecuteCustomJobAsync(customJobId, parameters),
            cronExpression,
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Local,
                DisplayName = displayName
            });

        _logger.LogInformation($"Scheduled recurring job for CustomJob {customJobId}, RecurringJobId: {recurringJobId}, DisplayName: {displayName}, Cron: {cronExpression}");
        return recurringJobId;
    }

    /// <summary>
    /// 移除定时任务
    /// </summary>
    public void RemoveRecurringJob(string recurringJobId)
    {
        RecurringJob.RemoveIfExists(recurringJobId);
        _logger.LogInformation($"Removed recurring job: {recurringJobId}");
    }

    /// <summary>
    /// 执行自定义任务
    /// </summary>
    public async Task ExecuteCustomJobAsync(Guid customJobId, Dictionary<string, object>? parameters = null)
    {
        var customJob = await _customJobService.GetAllCustomJobsAsync(activeOnly: false);
        var job = customJob.FirstOrDefault(j => j.CustomJobId == customJobId);

        if (job == null)
        {
            _logger.LogError($"CustomJob {customJobId} not found");
            throw new Exception($"CustomJob {customJobId} not found");
        }

        // 生成唯一的业务ID
        var businessId = $"{job.JobType}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}";

        // 创建WebJob记录
        var webJob = await _jobService.CreateJobAsync(
            job.JobType,
            businessId,
            $"执行定制任务: {job.Name}",
            parameters ?? new Dictionary<string, object>()
        );

        try
        {
            await _jobService.UpdateJobStatusAsync(webJob.JobId, "Running");
            await _jobService.AddJobLogAsync(webJob.JobId, "Start", "Info", $"开始执行自定义任务: {job.Name}", null);

            _logger.LogInformation($"Executing CustomJob {customJobId} ({job.Name}), BusinessId: {businessId}");

            // 执行HTTP请求
            await ExecuteHttpRequestAsync(webJob.JobId, job, parameters);

            // 更新最后执行时间
            job.LastExecutionTime = DateTime.UtcNow;
            if (job.EnableSchedule && !string.IsNullOrEmpty(job.CronExpression))
            {
                job.NextExecutionTime = GetNextExecutionTime(job.CronExpression);
            }
            await _customJobService.UpdateCustomJobAsync(job);

            await _jobService.UpdateJobStatusAsync(webJob.JobId, "Completed");
            await _jobService.AddJobLogAsync(webJob.JobId, "Complete", "Info", "任务执行完成", null);

            _logger.LogInformation($"CustomJob {customJobId} executed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CustomJob {customJobId} execution failed");
            await _jobService.UpdateJobStatusAsync(webJob.JobId, "Failed", ex.Message);
            await _jobService.AddJobLogAsync(webJob.JobId, "Error", "Error", ex.Message, new { StackTrace = ex.StackTrace });
            throw;
        }
    }

    private async Task ExecuteHttpRequestAsync(Guid jobId, Models.CustomJob customJob, Dictionary<string, object>? parameters)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromMinutes(5);

        await _jobService.AddJobLogAsync(jobId, "PrepareRequest", "Info", $"准备HTTP请求: {customJob.HttpMethod} {customJob.HttpUrl}", null);

        // 配置认证
        if (!string.IsNullOrEmpty(customJob.AuthType) && !string.IsNullOrEmpty(customJob.AuthConfig))
        {
            var authConfig = JsonSerializer.Deserialize<Dictionary<string, string>>(customJob.AuthConfig);
            if (authConfig != null)
            {
                ConfigureAuthentication(httpClient, customJob.AuthType, authConfig);
                await _jobService.AddJobLogAsync(jobId, "ConfigureAuth", "Info", $"配置认证: {customJob.AuthType}", null);
            }
        }

        // 添加自定义Headers
        if (!string.IsNullOrEmpty(customJob.Headers))
        {
            var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(customJob.Headers);
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
                await _jobService.AddJobLogAsync(jobId, "ConfigureHeaders", "Info", $"配置Headers: {headers.Count}个", null);
            }
        }

        // 准备请求
        var requestMessage = new HttpRequestMessage(
            new HttpMethod(customJob.HttpMethod),
            customJob.HttpUrl);

        // 准备请求体
        string? requestBody = null;
        if (parameters != null && parameters.Count > 0)
        {
            requestBody = JsonSerializer.Serialize(parameters);
        }
        else if (!string.IsNullOrEmpty(customJob.RequestBody))
        {
            requestBody = customJob.RequestBody;
        }
        else if (!string.IsNullOrEmpty(customJob.DefaultParameters))
        {
            requestBody = customJob.DefaultParameters;
        }

        if (!string.IsNullOrEmpty(requestBody))
        {
            requestMessage.Content = new StringContent(
                requestBody,
                Encoding.UTF8,
                "application/json");
            await _jobService.AddJobLogAsync(jobId, "RequestBody", "Info", $"请求体: {requestBody}", null);
        }

        // 发送请求
        await _jobService.AddJobLogAsync(jobId, "SendRequest", "Info", "发送HTTP请求...", null);
        var startTime = DateTime.UtcNow;
        var response = await httpClient.SendAsync(requestMessage);
        var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

        var responseContent = await response.Content.ReadAsStringAsync();

        await _jobService.AddJobLogAsync(
            jobId,
            "HttpResponse",
            response.IsSuccessStatusCode ? "Info" : "Warning",
            $"HTTP {customJob.HttpMethod} to {customJob.HttpUrl}",
            new
            {
                StatusCode = (int)response.StatusCode,
                StatusCodeText = response.StatusCode.ToString(),
                Duration = $"{duration}ms",
                Response = responseContent.Length > 1000 ? responseContent.Substring(0, 1000) + "..." : responseContent
            });

        // 检查断言
        if (!string.IsNullOrEmpty(customJob.AssertionExpression))
        {
            await _jobService.AddJobLogAsync(jobId, "CheckAssertion", "Info", $"检查断言: {customJob.AssertionExpression}", null);
            // TODO: 实现断言逻辑
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"HTTP请求失败: {response.StatusCode} - {responseContent}");
        }
    }

    private void ConfigureAuthentication(HttpClient httpClient, string authType, Dictionary<string, string> authConfig)
    {
        switch (authType.ToLower())
        {
            case "bearer":
                if (authConfig.TryGetValue("token", out var token))
                {
                    httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
                break;

            case "basic":
                if (authConfig.TryGetValue("username", out var username) &&
                    authConfig.TryGetValue("password", out var password))
                {
                    var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
                    httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
                }
                break;

            case "apikey":
                if (authConfig.TryGetValue("headerName", out var headerName) &&
                    authConfig.TryGetValue("apiKey", out var apiKey))
                {
                    httpClient.DefaultRequestHeaders.Add(headerName, apiKey);
                }
                break;
        }
    }

    private DateTime? GetNextExecutionTime(string cronExpression)
    {
        try
        {
            // 使用Cronos库解析Cron表达式
            var cron = Cronos.CronExpression.Parse(cronExpression);
            return cron.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Local);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to parse cron expression: {cronExpression}");
            return null;
        }
    }
}
