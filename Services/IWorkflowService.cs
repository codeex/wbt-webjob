using WbtWebJob.Models;

namespace WbtWebJob.Services;

/// <summary>
/// 工作流服务接口
/// </summary>
public interface IWorkflowService
{
    /// <summary>
    /// 创建工作流
    /// </summary>
    Task<Workflow> CreateWorkflowAsync(Workflow workflow);

    /// <summary>
    /// 获取工作流（包含节点和边）
    /// </summary>
    Task<Workflow?> GetWorkflowByIdAsync(Guid workflowId);

    /// <summary>
    /// 获取所有工作流
    /// </summary>
    Task<IEnumerable<Workflow>> GetAllWorkflowsAsync(bool activeOnly = false);

    /// <summary>
    /// 更新工作流
    /// </summary>
    Task<bool> UpdateWorkflowAsync(Workflow workflow);

    /// <summary>
    /// 删除工作流
    /// </summary>
    Task<bool> DeleteWorkflowAsync(Guid workflowId);

    /// <summary>
    /// 保存工作流为XML文件
    /// </summary>
    Task<string> SaveWorkflowToXmlAsync(Guid workflowId, string? filePath = null);

    /// <summary>
    /// 从XML文件导入工作流
    /// </summary>
    Task<Workflow> ImportWorkflowFromXmlAsync(string xmlContent);

    /// <summary>
    /// 添加节点到工作流
    /// </summary>
    Task<WorkflowNode> AddNodeAsync(Guid workflowId, WorkflowNode node);

    /// <summary>
    /// 更新节点
    /// </summary>
    Task<bool> UpdateNodeAsync(WorkflowNode node);

    /// <summary>
    /// 删除节点
    /// </summary>
    Task<bool> DeleteNodeAsync(Guid nodeId);

    /// <summary>
    /// 添加边到工作流
    /// </summary>
    Task<WorkflowEdge> AddEdgeAsync(Guid workflowId, WorkflowEdge edge);

    /// <summary>
    /// 更新边
    /// </summary>
    Task<bool> UpdateEdgeAsync(WorkflowEdge edge);

    /// <summary>
    /// 删除边
    /// </summary>
    Task<bool> DeleteEdgeAsync(Guid edgeId);

    /// <summary>
    /// 验证工作流（检查DAG是否有环）
    /// </summary>
    Task<(bool IsValid, string? ErrorMessage)> ValidateWorkflowAsync(Guid workflowId);

    /// <summary>
    /// 获取工作流的拓扑排序
    /// </summary>
    Task<List<WorkflowNode>> GetTopologicalOrderAsync(Guid workflowId);
}
