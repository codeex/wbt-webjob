using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WbtWebJob.Models;

/// <summary>
/// DAG工作流实体
/// </summary>
public class Workflow
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid WorkflowId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// XML文件内容（存储完整的DAG定义）
    /// </summary>
    public string? XmlContent { get; set; }

    /// <summary>
    /// XML文件路径（可选，用于文件系统存储）
    /// </summary>
    [MaxLength(500)]
    public string? XmlFilePath { get; set; }

    /// <summary>
    /// 工作流版本号
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Cron表达式，用于定时执行工作流
    /// </summary>
    [MaxLength(100)]
    public string? CronExpression { get; set; }

    /// <summary>
    /// 是否启用定时调度
    /// </summary>
    public bool EnableSchedule { get; set; } = false;

    /// <summary>
    /// 下次执行时间
    /// </summary>
    public DateTime? NextExecutionTime { get; set; }

    /// <summary>
    /// 上次执行时间
    /// </summary>
    public DateTime? LastExecutionTime { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 工作流节点集合
    /// </summary>
    public virtual ICollection<WorkflowNode> Nodes { get; set; } = new List<WorkflowNode>();

    /// <summary>
    /// 工作流边集合（节点之间的连接）
    /// </summary>
    public virtual ICollection<WorkflowEdge> Edges { get; set; } = new List<WorkflowEdge>();
}
