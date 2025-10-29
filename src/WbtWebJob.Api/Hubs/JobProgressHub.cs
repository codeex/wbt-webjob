using Microsoft.AspNetCore.SignalR;

namespace WbtWebJob.Api.Hubs;

/// <summary>
/// SignalR Hub for job progress tracking
/// </summary>
public class JobProgressHub : Hub
{
    private readonly ILogger<JobProgressHub> _logger;

    public JobProgressHub(ILogger<JobProgressHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 订阅Job进度更新
    /// </summary>
    public async Task SubscribeToJob(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"job_{jobId}");
        _logger.LogInformation("Client {ConnectionId} subscribed to job {JobId}", Context.ConnectionId, jobId);
    }

    /// <summary>
    /// 取消订阅Job进度更新
    /// </summary>
    public async Task UnsubscribeFromJob(string jobId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"job_{jobId}");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from job {JobId}", Context.ConnectionId, jobId);
    }

    /// <summary>
    /// 发送Job进度更新到订阅的客户端
    /// </summary>
    public static async Task SendJobProgress(IHubContext<JobProgressHub> hubContext, string jobId, object progressData)
    {
        await hubContext.Clients.Group($"job_{jobId}").SendAsync("JobProgress", progressData);
    }

    /// <summary>
    /// 发送Job完成通知
    /// </summary>
    public static async Task SendJobCompleted(IHubContext<JobProgressHub> hubContext, string jobId, object resultData)
    {
        await hubContext.Clients.Group($"job_{jobId}").SendAsync("JobCompleted", resultData);
    }

    /// <summary>
    /// 发送Job失败通知
    /// </summary>
    public static async Task SendJobFailed(IHubContext<JobProgressHub> hubContext, string jobId, object errorData)
    {
        await hubContext.Clients.Group($"job_{jobId}").SendAsync("JobFailed", errorData);
    }
}
