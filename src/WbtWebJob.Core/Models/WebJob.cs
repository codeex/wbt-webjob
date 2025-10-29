using WbtWebJob.Core.Enums;

namespace WbtWebJob.Core.Models;

public class WebJob
{
    /// <summary>
    /// Job ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 业务ID（由调用方传入）
    /// </summary>
    public string BusinessId { get; set; } = string.Empty;

    /// <summary>
    /// Job类型
    /// </summary>
    public JobType JobType { get; set; }

    /// <summary>
    /// Job名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Job描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Job状态
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// Job配置（JSON格式）
    /// </summary>
    public string? Configuration { get; set; }

    /// <summary>
    /// Hangfire Job ID
    /// </summary>
    public string? HangfireJobId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 开始执行时间
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Job日志
    /// </summary>
    public List<JobLog> Logs { get; set; } = new();
}
