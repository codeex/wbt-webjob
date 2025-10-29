using WbtWebJob.Core.Enums;

namespace WbtWebJob.Core.DTOs;

public class CreateJobRequest
{
    /// <summary>
    /// 业务ID
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
    /// Job配置（JSON格式）
    /// </summary>
    public string? Configuration { get; set; }
}
