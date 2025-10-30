using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WbtWebJob.Models;

/// <summary>
/// 工作流边（连接）实体
/// 表示节点之间的连接关系
/// </summary>
public class WorkflowEdge
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid EdgeId { get; set; }

    [Required]
    public Guid WorkflowId { get; set; }

    /// <summary>
    /// 源节点ID
    /// </summary>
    [Required]
    public Guid SourceNodeId { get; set; }

    /// <summary>
    /// 目标节点ID
    /// </summary>
    [Required]
    public Guid TargetNodeId { get; set; }

    /// <summary>
    /// 边的标签（可选）
    /// 例如：条件节点的true/false分支
    /// </summary>
    [MaxLength(100)]
    public string? Label { get; set; }

    /// <summary>
    /// 条件表达式（可选）
    /// 用于条件分支，决定是否执行该路径
    /// </summary>
    [MaxLength(500)]
    public string? Condition { get; set; }

    /// <summary>
    /// 边的优先级（用于排序执行顺序）
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// 边的样式配置（可选，用于前端渲染）
    /// </summary>
    public string? StyleConfig { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 导航属性：所属工作流
    /// </summary>
    [ForeignKey(nameof(WorkflowId))]
    public virtual Workflow? Workflow { get; set; }

    /// <summary>
    /// 导航属性：源节点
    /// </summary>
    [ForeignKey(nameof(SourceNodeId))]
    public virtual WorkflowNode? SourceNode { get; set; }

    /// <summary>
    /// 导航属性：目标节点
    /// </summary>
    [ForeignKey(nameof(TargetNodeId))]
    public virtual WorkflowNode? TargetNode { get; set; }
}
