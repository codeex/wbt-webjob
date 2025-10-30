using Microsoft.EntityFrameworkCore;
using WbtWebJob.Data;
using WbtWebJob.Models;

namespace WbtWebJob.Services;

/// <summary>
/// 工作流服务实现
/// </summary>
public class WorkflowService : IWorkflowService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkflowXmlService _xmlService;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(
        ApplicationDbContext context,
        IWorkflowXmlService xmlService,
        ILogger<WorkflowService> logger)
    {
        _context = context;
        _xmlService = xmlService;
        _logger = logger;
    }

    public async Task<Workflow> CreateWorkflowAsync(Workflow workflow)
    {
        try
        {
            workflow.CreatedAt = DateTime.UtcNow;
            workflow.UpdatedAt = DateTime.UtcNow;

            // 生成XML内容
            workflow.XmlContent = await _xmlService.SerializeToXmlAsync(workflow);

            _context.Workflows.Add(workflow);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created workflow {WorkflowId}: {Name}", workflow.WorkflowId, workflow.Name);

            return workflow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create workflow");
            throw;
        }
    }

    public async Task<Workflow?> GetWorkflowByIdAsync(Guid workflowId)
    {
        return await _context.Workflows
            .Include(w => w.Nodes)
            .Include(w => w.Edges)
            .FirstOrDefaultAsync(w => w.WorkflowId == workflowId);
    }

    public async Task<IEnumerable<Workflow>> GetAllWorkflowsAsync(bool activeOnly = false)
    {
        var query = _context.Workflows
            .Include(w => w.Nodes)
            .Include(w => w.Edges)
            .AsQueryable();

        if (activeOnly)
        {
            query = query.Where(w => w.IsActive);
        }

        return await query.OrderByDescending(w => w.CreatedAt).ToListAsync();
    }

    public async Task<bool> UpdateWorkflowAsync(Workflow workflow)
    {
        try
        {
            var existing = await _context.Workflows
                .Include(w => w.Nodes)
                .Include(w => w.Edges)
                .FirstOrDefaultAsync(w => w.WorkflowId == workflow.WorkflowId);

            if (existing == null)
            {
                return false;
            }

            // 更新基本信息
            existing.Name = workflow.Name;
            existing.Description = workflow.Description;
            existing.IsActive = workflow.IsActive;
            existing.CronExpression = workflow.CronExpression;
            existing.EnableSchedule = workflow.EnableSchedule;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.Version++;

            // 重新生成XML内容
            existing.XmlContent = await _xmlService.SerializeToXmlAsync(existing);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated workflow {WorkflowId}", workflow.WorkflowId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update workflow {WorkflowId}", workflow.WorkflowId);
            throw;
        }
    }

    public async Task<bool> DeleteWorkflowAsync(Guid workflowId)
    {
        try
        {
            var workflow = await _context.Workflows.FindAsync(workflowId);
            if (workflow == null)
            {
                return false;
            }

            _context.Workflows.Remove(workflow);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted workflow {WorkflowId}", workflowId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete workflow {WorkflowId}", workflowId);
            throw;
        }
    }

    public async Task<string> SaveWorkflowToXmlAsync(Guid workflowId, string? filePath = null)
    {
        var workflow = await GetWorkflowByIdAsync(workflowId);
        if (workflow == null)
        {
            throw new InvalidOperationException($"Workflow {workflowId} not found");
        }

        var savedPath = await _xmlService.SaveToFileAsync(workflow, filePath);

        // 更新文件路径
        workflow.XmlFilePath = savedPath;
        workflow.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return savedPath;
    }

    public async Task<Workflow> ImportWorkflowFromXmlAsync(string xmlContent)
    {
        // 验证XML
        var (isValid, errorMessage) = await _xmlService.ValidateXmlAsync(xmlContent);
        if (!isValid)
        {
            throw new InvalidOperationException($"Invalid XML: {errorMessage}");
        }

        // 反序列化
        var workflow = await _xmlService.DeserializeFromXmlAsync(xmlContent);

        // 生成新的ID（避免冲突）
        workflow.WorkflowId = Guid.NewGuid();
        foreach (var node in workflow.Nodes)
        {
            var oldNodeId = node.NodeId;
            node.NodeId = Guid.NewGuid();
            node.WorkflowId = workflow.WorkflowId;

            // 更新边中的节点引用
            foreach (var edge in workflow.Edges.Where(e => e.SourceNodeId == oldNodeId))
            {
                edge.SourceNodeId = node.NodeId;
            }
            foreach (var edge in workflow.Edges.Where(e => e.TargetNodeId == oldNodeId))
            {
                edge.TargetNodeId = node.NodeId;
            }
        }

        foreach (var edge in workflow.Edges)
        {
            edge.EdgeId = Guid.NewGuid();
            edge.WorkflowId = workflow.WorkflowId;
        }

        // 保存到数据库
        return await CreateWorkflowAsync(workflow);
    }

    public async Task<WorkflowNode> AddNodeAsync(Guid workflowId, WorkflowNode node)
    {
        var workflow = await GetWorkflowByIdAsync(workflowId);
        if (workflow == null)
        {
            throw new InvalidOperationException($"Workflow {workflowId} not found");
        }

        node.WorkflowId = workflowId;
        node.CreatedAt = DateTime.UtcNow;

        _context.WorkflowNodes.Add(node);
        await _context.SaveChangesAsync();

        // 更新工作流XML
        await UpdateWorkflowXmlAsync(workflowId);

        _logger.LogInformation("Added node {NodeId} to workflow {WorkflowId}", node.NodeId, workflowId);

        return node;
    }

    public async Task<bool> UpdateNodeAsync(WorkflowNode node)
    {
        try
        {
            var existing = await _context.WorkflowNodes.FindAsync(node.NodeId);
            if (existing == null)
            {
                return false;
            }

            existing.NodeType = node.NodeType;
            existing.Name = node.Name;
            existing.Description = node.Description;
            existing.Configuration = node.Configuration;
            existing.PositionX = node.PositionX;
            existing.PositionY = node.PositionY;
            existing.StyleConfig = node.StyleConfig;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // 更新工作流XML
            await UpdateWorkflowXmlAsync(existing.WorkflowId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update node {NodeId}", node.NodeId);
            throw;
        }
    }

    public async Task<bool> DeleteNodeAsync(Guid nodeId)
    {
        try
        {
            var node = await _context.WorkflowNodes.FindAsync(nodeId);
            if (node == null)
            {
                return false;
            }

            var workflowId = node.WorkflowId;

            // 删除相关的边
            var edges = await _context.WorkflowEdges
                .Where(e => e.SourceNodeId == nodeId || e.TargetNodeId == nodeId)
                .ToListAsync();

            _context.WorkflowEdges.RemoveRange(edges);
            _context.WorkflowNodes.Remove(node);

            await _context.SaveChangesAsync();

            // 更新工作流XML
            await UpdateWorkflowXmlAsync(workflowId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete node {NodeId}", nodeId);
            throw;
        }
    }

    public async Task<WorkflowEdge> AddEdgeAsync(Guid workflowId, WorkflowEdge edge)
    {
        var workflow = await GetWorkflowByIdAsync(workflowId);
        if (workflow == null)
        {
            throw new InvalidOperationException($"Workflow {workflowId} not found");
        }

        // 验证节点存在
        var sourceExists = await _context.WorkflowNodes.AnyAsync(n => n.NodeId == edge.SourceNodeId);
        var targetExists = await _context.WorkflowNodes.AnyAsync(n => n.NodeId == edge.TargetNodeId);

        if (!sourceExists || !targetExists)
        {
            throw new InvalidOperationException("Source or target node not found");
        }

        edge.WorkflowId = workflowId;
        edge.CreatedAt = DateTime.UtcNow;

        _context.WorkflowEdges.Add(edge);
        await _context.SaveChangesAsync();

        // 检查是否产生环
        var (isValid, errorMessage) = await ValidateWorkflowAsync(workflowId);
        if (!isValid)
        {
            // 回滚
            _context.WorkflowEdges.Remove(edge);
            await _context.SaveChangesAsync();
            throw new InvalidOperationException($"Adding this edge would create a cycle: {errorMessage}");
        }

        // 更新工作流XML
        await UpdateWorkflowXmlAsync(workflowId);

        _logger.LogInformation("Added edge {EdgeId} to workflow {WorkflowId}", edge.EdgeId, workflowId);

        return edge;
    }

    public async Task<bool> UpdateEdgeAsync(WorkflowEdge edge)
    {
        try
        {
            var existing = await _context.WorkflowEdges.FindAsync(edge.EdgeId);
            if (existing == null)
            {
                return false;
            }

            existing.Label = edge.Label;
            existing.Condition = edge.Condition;
            existing.Priority = edge.Priority;
            existing.StyleConfig = edge.StyleConfig;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // 更新工作流XML
            await UpdateWorkflowXmlAsync(existing.WorkflowId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update edge {EdgeId}", edge.EdgeId);
            throw;
        }
    }

    public async Task<bool> DeleteEdgeAsync(Guid edgeId)
    {
        try
        {
            var edge = await _context.WorkflowEdges.FindAsync(edgeId);
            if (edge == null)
            {
                return false;
            }

            var workflowId = edge.WorkflowId;

            _context.WorkflowEdges.Remove(edge);
            await _context.SaveChangesAsync();

            // 更新工作流XML
            await UpdateWorkflowXmlAsync(workflowId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete edge {EdgeId}", edgeId);
            throw;
        }
    }

    public async Task<(bool IsValid, string? ErrorMessage)> ValidateWorkflowAsync(Guid workflowId)
    {
        var workflow = await GetWorkflowByIdAsync(workflowId);
        if (workflow == null)
        {
            return (false, "Workflow not found");
        }

        // 检查是否有环（DAG验证）
        var hasCycle = HasCycle(workflow);
        if (hasCycle)
        {
            return (false, "Workflow contains a cycle (not a DAG)");
        }

        // 检查节点配置是否有效
        foreach (var node in workflow.Nodes)
        {
            if (string.IsNullOrEmpty(node.Name))
            {
                return (false, $"Node {node.NodeId} has no name");
            }

            if (string.IsNullOrEmpty(node.NodeType))
            {
                return (false, $"Node {node.NodeId} has no type");
            }
        }

        return (true, null);
    }

    public async Task<List<WorkflowNode>> GetTopologicalOrderAsync(Guid workflowId)
    {
        var workflow = await GetWorkflowByIdAsync(workflowId);
        if (workflow == null)
        {
            throw new InvalidOperationException($"Workflow {workflowId} not found");
        }

        return TopologicalSort(workflow);
    }

    #region Private Methods

    private async Task UpdateWorkflowXmlAsync(Guid workflowId)
    {
        var workflow = await GetWorkflowByIdAsync(workflowId);
        if (workflow != null)
        {
            workflow.XmlContent = await _xmlService.SerializeToXmlAsync(workflow);
            workflow.UpdatedAt = DateTime.UtcNow;
            workflow.Version++;
            await _context.SaveChangesAsync();
        }
    }

    private bool HasCycle(Workflow workflow)
    {
        var visited = new HashSet<Guid>();
        var recursionStack = new HashSet<Guid>();

        foreach (var node in workflow.Nodes)
        {
            if (HasCycleDFS(node.NodeId, workflow, visited, recursionStack))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasCycleDFS(Guid nodeId, Workflow workflow, HashSet<Guid> visited, HashSet<Guid> recursionStack)
    {
        if (recursionStack.Contains(nodeId))
        {
            return true; // 发现环
        }

        if (visited.Contains(nodeId))
        {
            return false; // 已经访问过且无环
        }

        visited.Add(nodeId);
        recursionStack.Add(nodeId);

        // 访问所有邻接节点
        var neighbors = workflow.Edges
            .Where(e => e.SourceNodeId == nodeId)
            .Select(e => e.TargetNodeId);

        foreach (var neighbor in neighbors)
        {
            if (HasCycleDFS(neighbor, workflow, visited, recursionStack))
            {
                return true;
            }
        }

        recursionStack.Remove(nodeId);
        return false;
    }

    private List<WorkflowNode> TopologicalSort(Workflow workflow)
    {
        var result = new List<WorkflowNode>();
        var visited = new HashSet<Guid>();
        var stack = new Stack<Guid>();

        foreach (var node in workflow.Nodes)
        {
            if (!visited.Contains(node.NodeId))
            {
                TopologicalSortDFS(node.NodeId, workflow, visited, stack);
            }
        }

        // 将栈中的节点ID转换为节点对象
        while (stack.Count > 0)
        {
            var nodeId = stack.Pop();
            var node = workflow.Nodes.FirstOrDefault(n => n.NodeId == nodeId);
            if (node != null)
            {
                result.Add(node);
            }
        }

        return result;
    }

    private void TopologicalSortDFS(Guid nodeId, Workflow workflow, HashSet<Guid> visited, Stack<Guid> stack)
    {
        visited.Add(nodeId);

        // 访问所有邻接节点
        var neighbors = workflow.Edges
            .Where(e => e.SourceNodeId == nodeId)
            .Select(e => e.TargetNodeId);

        foreach (var neighbor in neighbors)
        {
            if (!visited.Contains(neighbor))
            {
                TopologicalSortDFS(neighbor, workflow, visited, stack);
            }
        }

        stack.Push(nodeId);
    }

    #endregion
}
