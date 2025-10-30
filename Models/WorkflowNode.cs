using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WbtWebJob.Models;

/// <summary>
/// 工作流节点
/// </summary>
public class WorkflowNode
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid NodeId { get; set; }

    [Required]
    public Guid CustomJobId { get; set; }

    [ForeignKey("CustomJobId")]
    public CustomJob? CustomJob { get; set; }

    /// <summary>
    /// 节点类型
    /// </summary>
    [Required]
    public WorkflowNodeType NodeType { get; set; }

    /// <summary>
    /// 节点名称（不允许包含.）
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 节点在画布上的X坐标
    /// </summary>
    public double PositionX { get; set; }

    /// <summary>
    /// 节点在画布上的Y坐标
    /// </summary>
    public double PositionY { get; set; }

    /// <summary>
    /// 节点配置（JSON格式），包含节点特定的属性
    /// </summary>
    public string? Configuration { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
