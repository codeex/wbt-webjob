using Microsoft.AspNetCore.SignalR;
using System.Text;
using System.Text.Json;
using WbtWebJob.Hubs;

namespace WbtWebJob.Services;

public class JobExecutor : IJobExecutor
{
    private readonly IJobService _jobService;
    private readonly ICustomJobService _customJobService;
    private readonly IHubContext<JobProgressHub> _hubContext;
    private readonly IHttpClientFactory _httpClientFactory;

    public JobExecutor(
        IJobService jobService,
        ICustomJobService customJobService,
        IHubContext<JobProgressHub> hubContext,
        IHttpClientFactory httpClientFactory)
    {
        _jobService = jobService;
        _customJobService = customJobService;
        _hubContext = hubContext;
        _httpClientFactory = httpClientFactory;
    }

    public async Task ExecuteJobAsync(Guid jobId)
    {
        var job = await _jobService.GetJobByIdAsync(jobId);
        if (job == null)
        {
            throw new Exception($"Job {jobId} not found");
        }

        try
        {
            await _jobService.UpdateJobStatusAsync(jobId, "Running");
            await _jobService.AddJobLogAsync(jobId, "Start", "Info", "Job execution started", null);
            await NotifyProgress(job.BusinessId, "started", 0);

            // 检查是否为自定义Job
            var customJob = await _customJobService.GetCustomJobByTypeAsync(job.JobType);
            if (customJob != null)
            {
                await ExecuteCustomJobAsync(jobId, job, customJob);
            }
            else
            {
                await ExecuteBuiltInJobAsync(jobId, job);
            }

            await _jobService.UpdateJobStatusAsync(jobId, "Completed");
            await _jobService.AddJobLogAsync(jobId, "Complete", "Info", "Job execution completed", null);
            await NotifyProgress(job.BusinessId, "completed", 100);
        }
        catch (Exception ex)
        {
            await _jobService.UpdateJobStatusAsync(jobId, "Failed", ex.Message);
            await _jobService.AddJobLogAsync(jobId, "Error", "Error", ex.Message, new { StackTrace = ex.StackTrace });
            await NotifyProgress(job.BusinessId, "failed", 0, ex.Message);
        }
    }

    private async Task ExecuteCustomJobAsync(Guid jobId, Models.WebJob job, Models.CustomJob customJob)
    {
        await _jobService.AddJobLogAsync(jobId, "ExecuteCustomJob", "Info", $"Executing custom job: {customJob.Name}", null);

        var httpClient = _httpClientFactory.CreateClient();

        // 配置认证
        if (!string.IsNullOrEmpty(customJob.AuthType) && !string.IsNullOrEmpty(customJob.AuthConfig))
        {
            var authConfig = JsonSerializer.Deserialize<Dictionary<string, string>>(customJob.AuthConfig);
            if (authConfig != null)
            {
                ConfigureAuthentication(httpClient, customJob.AuthType, authConfig);
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
            }
        }

        // 准备请求
        var requestMessage = new HttpRequestMessage(
            new HttpMethod(customJob.HttpMethod),
            customJob.HttpUrl);

        if (!string.IsNullOrEmpty(job.JobParameters))
        {
            requestMessage.Content = new StringContent(
                job.JobParameters,
                Encoding.UTF8,
                "application/json");
        }

        // 发送请求
        var response = await httpClient.SendAsync(requestMessage);
        var responseContent = await response.Content.ReadAsStringAsync();

        await _jobService.AddJobLogAsync(
            jobId,
            "HttpResponse",
            response.IsSuccessStatusCode ? "Info" : "Error",
            $"HTTP {customJob.HttpMethod} to {customJob.HttpUrl}",
            new
            {
                StatusCode = (int)response.StatusCode,
                Response = responseContent
            });

        response.EnsureSuccessStatusCode();
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

    private async Task ExecuteBuiltInJobAsync(Guid jobId, Models.WebJob job)
    {
        await _jobService.AddJobLogAsync(jobId, "ExecuteBuiltInJob", "Info", $"Executing built-in job: {job.JobType}", null);

        // 这里可以添加内置Job的实现逻辑
        // 例如：数据导出、报表生成、数据清理等

        await Task.Delay(1000); // 模拟处理时间
    }

    private async Task NotifyProgress(string businessId, string status, int progress, string? message = null)
    {
        await _hubContext.Clients.Group(businessId).SendAsync("JobProgress", new
        {
            BusinessId = businessId,
            Status = status,
            Progress = progress,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }
}
