using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WbtWebJob.Models;

/// <summary>
/// 工作流连接线
/// </summary>
public class WorkflowConnection
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid ConnectionId { get; set; }

    [Required]
    public Guid CustomJobId { get; set; }

    [ForeignKey("CustomJobId")]
    public CustomJob? CustomJob { get; set; }

    /// <summary>
    /// 源节点ID
    /// </summary>
    [Required]
    public Guid SourceNodeId { get; set; }

    [ForeignKey("SourceNodeId")]
    public WorkflowNode? SourceNode { get; set; }

    /// <summary>
    /// 目标节点ID
    /// </summary>
    [Required]
    public Guid TargetNodeId { get; set; }

    [ForeignKey("TargetNodeId")]
    public WorkflowNode? TargetNode { get; set; }

    /// <summary>
    /// 源节点输出端口（对于条件节点：true/false）
    /// </summary>
    [MaxLength(50)]
    public string? SourcePort { get; set; }

    /// <summary>
    /// 目标节点输入端口
    /// </summary>
    [MaxLength(50)]
    public string? TargetPort { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
