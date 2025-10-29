namespace WbtWebJob.Core.Models;

public class JobLog
{
    /// <summary>
    /// 日志ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Job ID
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// 日志级别
    /// </summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>
    /// 日志消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 详细信息
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// 步骤名称
    /// </summary>
    public string? StepName { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 关联的Job
    /// </summary>
    public WebJob? Job { get; set; }
}
