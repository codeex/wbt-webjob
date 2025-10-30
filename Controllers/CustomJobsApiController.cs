using Microsoft.AspNetCore.Mvc;
using WbtWebJob.Models;
using WbtWebJob.Services;
using WbtWebJob.Data;
using Microsoft.EntityFrameworkCore;

namespace WbtWebJob.Controllers;

[ApiController]
[Route("api/customjobs")]
public class CustomJobsApiController : ControllerBase
{
    private readonly ICustomJobService _customJobService;
    private readonly ApplicationDbContext _context;

    public CustomJobsApiController(ICustomJobService customJobService, ApplicationDbContext context)
    {
        _customJobService = customJobService;
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomJob([FromBody] CustomJob customJob)
    {
        if (string.IsNullOrEmpty(customJob.JobType) || string.IsNullOrEmpty(customJob.HttpUrl))
        {
            return BadRequest(new { error = "JobType and HttpUrl are required" });
        }

        var created = await _customJobService.CreateCustomJobAsync(customJob);
        return Ok(created);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCustomJobs([FromQuery] bool includeInactive = false)
    {
        var customJobs = await _customJobService.GetAllCustomJobsAsync(!includeInactive);
        return Ok(customJobs);
    }

    [HttpGet("{jobType}")]
    public async Task<IActionResult> GetCustomJobByType(string jobType)
    {
        var customJob = await _customJobService.GetCustomJobByTypeAsync(jobType);
        if (customJob == null)
        {
            return NotFound(new { error = "Custom job not found" });
        }

        return Ok(customJob);
    }

    [HttpPut("{customJobId}")]
    public async Task<IActionResult> UpdateCustomJob(Guid customJobId, [FromBody] CustomJob customJob)
    {
        customJob.CustomJobId = customJobId;
        var updated = await _customJobService.UpdateCustomJobAsync(customJob);

        if (!updated)
        {
            return NotFound(new { error = "Custom job not found" });
        }

        return Ok(new { message = "Custom job updated successfully" });
    }

    [HttpDelete("{customJobId}")]
    public async Task<IActionResult> DeleteCustomJob(Guid customJobId)
    {
        var deleted = await _customJobService.DeleteCustomJobAsync(customJobId);

        if (!deleted)
        {
            return NotFound(new { error = "Custom job not found" });
        }

        return Ok(new { message = "Custom job deleted successfully" });
    }

    /// <summary>
    /// 保存工作流
    /// </summary>
    [HttpPost("workflow")]
    public async Task<IActionResult> SaveWorkflow([FromBody] WorkflowDto workflowDto)
    {
        if (string.IsNullOrEmpty(workflowDto.Name) || string.IsNullOrEmpty(workflowDto.JobType))
        {
            return BadRequest(new { error = "Name and JobType are required" });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            CustomJob customJob;
            bool isNew = workflowDto.CustomJobId == Guid.Empty;

            if (isNew)
            {
                // 创建新的CustomJob
                customJob = new CustomJob
                {
                    CustomJobId = Guid.NewGuid(),
                    Name = workflowDto.Name,
                    JobType = workflowDto.JobType,
                    Description = workflowDto.Description,
                    HttpUrl = "workflow://dag", // 标记为工作流类型
                    HttpMethod = "WORKFLOW",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CustomJobs.Add(customJob);
            }
            else
            {
                // 更新现有CustomJob
                customJob = await _context.CustomJobs.FindAsync(workflowDto.CustomJobId);
                if (customJob == null)
                {
                    return NotFound(new { error = "Custom job not found" });
                }

                customJob.Name = workflowDto.Name;
                customJob.JobType = workflowDto.JobType;
                customJob.Description = workflowDto.Description;
                customJob.UpdatedAt = DateTime.UtcNow;

                // 删除旧的节点和连接
                var oldNodes = await _context.WorkflowNodes
                    .Where(n => n.CustomJobId == customJob.CustomJobId)
                    .ToListAsync();
                _context.WorkflowNodes.RemoveRange(oldNodes);

                var oldConnections = await _context.WorkflowConnections
                    .Where(c => c.CustomJobId == customJob.CustomJobId)
                    .ToListAsync();
                _context.WorkflowConnections.RemoveRange(oldConnections);
            }

            await _context.SaveChangesAsync();

            // 保存节点
            var nodeIdMapping = new Dictionary<string, Guid>(); // 客户端ID -> 数据库ID
            foreach (var nodeDto in workflowDto.Nodes)
            {
                var nodeTypeEnum = Enum.Parse<WorkflowNodeType>(nodeDto.NodeType, true);
                var node = new WorkflowNode
                {
                    NodeId = Guid.NewGuid(),
                    CustomJobId = customJob.CustomJobId,
                    NodeType = nodeTypeEnum,
                    Name = nodeDto.Name,
                    PositionX = nodeDto.PositionX,
                    PositionY = nodeDto.PositionY,
                    Configuration = nodeDto.Configuration,
                    CreatedAt = DateTime.UtcNow
                };
                _context.WorkflowNodes.Add(node);
                nodeIdMapping[nodeDto.NodeId] = node.NodeId;
            }

            await _context.SaveChangesAsync();

            // 保存连接
            foreach (var connDto in workflowDto.Connections)
            {
                if (!nodeIdMapping.TryGetValue(connDto.SourceNodeId, out var sourceNodeId) ||
                    !nodeIdMapping.TryGetValue(connDto.TargetNodeId, out var targetNodeId))
                {
                    continue; // 跳过无效连接
                }

                var connection = new WorkflowConnection
                {
                    ConnectionId = Guid.NewGuid(),
                    CustomJobId = customJob.CustomJobId,
                    SourceNodeId = sourceNodeId,
                    TargetNodeId = targetNodeId,
                    SourcePort = connDto.SourcePort,
                    CreatedAt = DateTime.UtcNow
                };
                _context.WorkflowConnections.Add(connection);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                customJobId = customJob.CustomJobId,
                message = "Workflow saved successfully"
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { error = "Failed to save workflow", details = ex.Message });
        }
    }

    /// <summary>
    /// 加载工作流
    /// </summary>
    [HttpGet("workflow/{customJobId}")]
    public async Task<IActionResult> LoadWorkflow(Guid customJobId)
    {
        var customJob = await _context.CustomJobs.FindAsync(customJobId);
        if (customJob == null)
        {
            return NotFound(new { error = "Custom job not found" });
        }

        var nodes = await _context.WorkflowNodes
            .Where(n => n.CustomJobId == customJobId)
            .ToListAsync();

        var connections = await _context.WorkflowConnections
            .Where(c => c.CustomJobId == customJobId)
            .ToListAsync();

        var workflowDto = new WorkflowDto
        {
            CustomJobId = customJob.CustomJobId,
            Name = customJob.Name,
            JobType = customJob.JobType,
            Description = customJob.Description,
            Nodes = nodes.Select(n => new WorkflowNodeDto
            {
                NodeId = n.NodeId.ToString(),
                NodeType = n.NodeType.ToString(),
                Name = n.Name,
                PositionX = n.PositionX,
                PositionY = n.PositionY,
                Configuration = n.Configuration
            }).ToList(),
            Connections = connections.Select(c => new WorkflowConnectionDto
            {
                ConnectionId = c.ConnectionId.ToString(),
                SourceNodeId = c.SourceNodeId.ToString(),
                TargetNodeId = c.TargetNodeId.ToString(),
                SourcePort = c.SourcePort
            }).ToList()
        };

        return Ok(workflowDto);
    }
}
