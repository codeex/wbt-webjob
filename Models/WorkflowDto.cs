namespace WbtWebJob.Models;

/// <summary>
/// 工作流数据传输对象
/// </summary>
public class WorkflowDto
{
    public Guid CustomJobId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<WorkflowNodeDto> Nodes { get; set; } = new();
    public List<WorkflowConnectionDto> Connections { get; set; } = new();
}

/// <summary>
/// 工作流节点DTO
/// </summary>
public class WorkflowNodeDto
{
    public string NodeId { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public string? Configuration { get; set; }
}

/// <summary>
/// 工作流连接DTO
/// </summary>
public class WorkflowConnectionDto
{
    public string ConnectionId { get; set; } = string.Empty;
    public string SourceNodeId { get; set; } = string.Empty;
    public string TargetNodeId { get; set; } = string.Empty;
    public string? SourcePort { get; set; }
}
