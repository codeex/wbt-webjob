using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WbtWebJob.Models;

/// <summary>
/// 工作流节点实体
/// </summary>
public class WorkflowNode
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid NodeId { get; set; }

    [Required]
    public Guid WorkflowId { get; set; }

    /// <summary>
    /// 节点类型：HttpRequest, Condition, Delay, Script, StartNode, EndNode等
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string NodeType { get; set; } = string.Empty;

    /// <summary>
    /// 节点名称
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 节点描述
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 节点配置（JSON格式）
    /// 不同类型的节点有不同的配置：
    /// - HttpRequest: { url, method, headers, body, auth, ... }
    /// - Condition: { expression, trueOutput, falseOutput }
    /// - Delay: { delaySeconds }
    /// - Script: { scriptType, scriptContent }
    /// </summary>
    public string? Configuration { get; set; }

    /// <summary>
    /// 节点在编辑器中的X坐标
    /// </summary>
    public double PositionX { get; set; }

    /// <summary>
    /// 节点在编辑器中的Y坐标
    /// </summary>
    public double PositionY { get; set; }

    /// <summary>
    /// 节点样式配置（可选，用于前端渲染）
    /// </summary>
    public string? StyleConfig { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 导航属性：所属工作流
    /// </summary>
    [ForeignKey(nameof(WorkflowId))]
    public virtual Workflow? Workflow { get; set; }
}

/// <summary>
/// 节点类型常量
/// </summary>
public static class NodeTypes
{
    public const string StartNode = "StartNode";
    public const string EndNode = "EndNode";
    public const string HttpRequest = "HttpRequest";
    public const string Condition = "Condition";
    public const string Delay = "Delay";
    public const string Script = "Script";
    public const string Parallel = "Parallel";
    public const string SubWorkflow = "SubWorkflow";
}
