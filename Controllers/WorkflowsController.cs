using Microsoft.AspNetCore.Mvc;
using WbtWebJob.Models;
using WbtWebJob.Services;
using System.Text.Json;

namespace WbtWebJob.Controllers;

/// <summary>
/// MVC Controller for DAG Workflow Editor
/// </summary>
public class WorkflowsController : Controller
{
    private readonly IWorkflowService _workflowService;
    private readonly IWorkflowXmlService _xmlService;
    private readonly ILogger<WorkflowsController> _logger;

    public WorkflowsController(
        IWorkflowService workflowService,
        IWorkflowXmlService xmlService,
        ILogger<WorkflowsController> logger)
    {
        _workflowService = workflowService;
        _xmlService = xmlService;
        _logger = logger;
    }

    // GET: /Workflows
    public async Task<IActionResult> Index()
    {
        var workflows = await _workflowService.GetAllWorkflowsAsync(activeOnly: false);
        return View(workflows);
    }

    // GET: /Workflows/Editor?id={workflowId}
    public async Task<IActionResult> Editor(Guid? id)
    {
        Workflow? workflow = null;

        if (id.HasValue)
        {
            workflow = await _workflowService.GetWorkflowByIdAsync(id.Value);
            if (workflow == null)
            {
                return NotFound();
            }
        }

        return View(workflow);
    }

    // GET: /Workflows/Details/{id}
    public async Task<IActionResult> Details(Guid id)
    {
        var workflow = await _workflowService.GetWorkflowByIdAsync(id);
        if (workflow == null)
        {
            return NotFound();
        }

        return View(workflow);
    }

    // POST: /Workflows/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] Workflow workflow)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(workflow.Name))
            {
                return BadRequest(new { success = false, message = "工作流名称不能为空" });
            }

            var created = await _workflowService.CreateWorkflowAsync(workflow);
            return Json(new { success = true, workflowId = created.WorkflowId, message = "工作流创建成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create workflow");
            return BadRequest(new { success = false, message = $"创建失败: {ex.Message}" });
        }
    }

    // POST: /Workflows/Update
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromBody] Workflow workflow)
    {
        try
        {
            var success = await _workflowService.UpdateWorkflowAsync(workflow);
            if (!success)
            {
                return NotFound(new { success = false, message = "工作流不存在" });
            }

            return Json(new { success = true, message = "工作流更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update workflow {WorkflowId}", workflow.WorkflowId);
            return BadRequest(new { success = false, message = $"更新失败: {ex.Message}" });
        }
    }

    // POST: /Workflows/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var success = await _workflowService.DeleteWorkflowAsync(id);
            if (!success)
            {
                return NotFound(new { success = false, message = "工作流不存在" });
            }

            TempData["SuccessMessage"] = "工作流删除成功";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete workflow {WorkflowId}", id);
            TempData["ErrorMessage"] = $"删除失败: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: /Workflows/AddNode
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNode([FromBody] WorkflowNode node)
    {
        try
        {
            var created = await _workflowService.AddNodeAsync(node.WorkflowId, node);
            return Json(new { success = true, node = created });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add node to workflow {WorkflowId}", node.WorkflowId);
            return BadRequest(new { success = false, message = $"添加节点失败: {ex.Message}" });
        }
    }

    // POST: /Workflows/UpdateNode
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateNode([FromBody] WorkflowNode node)
    {
        try
        {
            var success = await _workflowService.UpdateNodeAsync(node);
            if (!success)
            {
                return NotFound(new { success = false, message = "节点不存在" });
            }

            return Json(new { success = true, message = "节点更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update node {NodeId}", node.NodeId);
            return BadRequest(new { success = false, message = $"更新节点失败: {ex.Message}" });
        }
    }

    // POST: /Workflows/DeleteNode/{nodeId}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteNode(Guid nodeId)
    {
        try
        {
            var success = await _workflowService.DeleteNodeAsync(nodeId);
            if (!success)
            {
                return NotFound(new { success = false, message = "节点不存在" });
            }

            return Json(new { success = true, message = "节点删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete node {NodeId}", nodeId);
            return BadRequest(new { success = false, message = $"删除节点失败: {ex.Message}" });
        }
    }

    // POST: /Workflows/AddEdge
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddEdge([FromBody] WorkflowEdge edge)
    {
        try
        {
            var created = await _workflowService.AddEdgeAsync(edge.WorkflowId, edge);
            return Json(new { success = true, edge = created });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add edge to workflow {WorkflowId}", edge.WorkflowId);
            return BadRequest(new { success = false, message = $"添加连接失败: {ex.Message}" });
        }
    }

    // POST: /Workflows/UpdateEdge
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEdge([FromBody] WorkflowEdge edge)
    {
        try
        {
            var success = await _workflowService.UpdateEdgeAsync(edge);
            if (!success)
            {
                return NotFound(new { success = false, message = "连接不存在" });
            }

            return Json(new { success = true, message = "连接更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update edge {EdgeId}", edge.EdgeId);
            return BadRequest(new { success = false, message = $"更新连接失败: {ex.Message}" });
        }
    }

    // POST: /Workflows/DeleteEdge/{edgeId}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEdge(Guid edgeId)
    {
        try
        {
            var success = await _workflowService.DeleteEdgeAsync(edgeId);
            if (!success)
            {
                return NotFound(new { success = false, message = "连接不存在" });
            }

            return Json(new { success = true, message = "连接删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete edge {EdgeId}", edgeId);
            return BadRequest(new { success = false, message = $"删除连接失败: {ex.Message}" });
        }
    }

    // GET: /Workflows/GetWorkflowData/{id}
    public async Task<IActionResult> GetWorkflowData(Guid id)
    {
        try
        {
            var workflow = await _workflowService.GetWorkflowByIdAsync(id);
            if (workflow == null)
            {
                return NotFound(new { success = false, message = "工作流不存在" });
            }

            return Json(new
            {
                success = true,
                workflow = new
                {
                    workflow.WorkflowId,
                    workflow.Name,
                    workflow.Description,
                    workflow.Version,
                    workflow.IsActive,
                    workflow.CronExpression,
                    workflow.EnableSchedule,
                    nodes = workflow.Nodes,
                    edges = workflow.Edges
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workflow data {WorkflowId}", id);
            return BadRequest(new { success = false, message = $"获取工作流数据失败: {ex.Message}" });
        }
    }

    // POST: /Workflows/SaveAsXml/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAsXml(Guid id)
    {
        try
        {
            var filePath = await _workflowService.SaveWorkflowToXmlAsync(id);
            return Json(new { success = true, filePath, message = "工作流已保存为XML文件" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save workflow {WorkflowId} as XML", id);
            return BadRequest(new { success = false, message = $"保存XML失败: {ex.Message}" });
        }
    }

    // POST: /Workflows/ImportFromXml
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportFromXml([FromBody] ImportXmlRequest request)
    {
        try
        {
            var workflow = await _workflowService.ImportWorkflowFromXmlAsync(request.XmlContent);
            return Json(new { success = true, workflowId = workflow.WorkflowId, message = "工作流导入成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import workflow from XML");
            return BadRequest(new { success = false, message = $"导入XML失败: {ex.Message}" });
        }
    }

    // GET: /Workflows/DownloadXml/{id}
    public async Task<IActionResult> DownloadXml(Guid id)
    {
        try
        {
            var workflow = await _workflowService.GetWorkflowByIdAsync(id);
            if (workflow == null)
            {
                return NotFound();
            }

            var xmlContent = await _xmlService.SerializeToXmlAsync(workflow);
            var fileName = $"{workflow.Name.Replace(" ", "_")}_{workflow.WorkflowId}.xml";

            return File(System.Text.Encoding.UTF8.GetBytes(xmlContent), "application/xml", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download workflow XML {WorkflowId}", id);
            TempData["ErrorMessage"] = $"下载失败: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: /Workflows/Validate/{id}
    [HttpPost]
    public async Task<IActionResult> Validate(Guid id)
    {
        try
        {
            var (isValid, errorMessage) = await _workflowService.ValidateWorkflowAsync(id);
            return Json(new { success = isValid, message = errorMessage ?? "工作流验证通过" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate workflow {WorkflowId}", id);
            return BadRequest(new { success = false, message = $"验证失败: {ex.Message}" });
        }
    }

    // POST: /Workflows/ToggleActive/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        try
        {
            var workflow = await _workflowService.GetWorkflowByIdAsync(id);
            if (workflow == null)
            {
                return NotFound(new { success = false, message = "工作流不存在" });
            }

            workflow.IsActive = !workflow.IsActive;
            await _workflowService.UpdateWorkflowAsync(workflow);

            return Json(new { success = true, isActive = workflow.IsActive });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle workflow active status {WorkflowId}", id);
            return BadRequest(new { success = false, message = $"操作失败: {ex.Message}" });
        }
    }
}

public class ImportXmlRequest
{
    public string XmlContent { get; set; } = string.Empty;
}
