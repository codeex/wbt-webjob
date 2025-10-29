namespace WbtWebJob.Services;

/// <summary>
/// Hangfire作业服务接口
/// </summary>
public interface IHangfireJobService
{
    /// <summary>
    /// 添加一次性测试任务到Hangfire
    /// </summary>
    string EnqueueTestJob(Guid customJobId, Dictionary<string, object>? parameters = null);

    /// <summary>
    /// 添加定时任务到Hangfire
    /// </summary>
    Task<string> ScheduleRecurringJob(Guid customJobId, string cronExpression, Dictionary<string, object>? parameters = null);

    /// <summary>
    /// 移除定时任务
    /// </summary>
    void RemoveRecurringJob(string recurringJobId);

    /// <summary>
    /// 执行自定义任务
    /// </summary>
    Task ExecuteCustomJobAsync(Guid customJobId, Dictionary<string, object>? parameters = null);
}
