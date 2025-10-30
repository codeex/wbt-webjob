using System.Xml.Linq;
using WbtWebJob.Models;

namespace WbtWebJob.Services;

/// <summary>
/// 工作流XML序列化/反序列化服务实现
/// </summary>
public class WorkflowXmlService : IWorkflowXmlService
{
    private readonly ILogger<WorkflowXmlService> _logger;
    private readonly string _workflowDirectory;

    public WorkflowXmlService(ILogger<WorkflowXmlService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _workflowDirectory = configuration["WorkflowXmlDirectory"] ?? "Workflows";

        // 确保目录存在
        if (!Directory.Exists(_workflowDirectory))
        {
            Directory.CreateDirectory(_workflowDirectory);
        }
    }

    public Task<string> SerializeToXmlAsync(Workflow workflow)
    {
        try
        {
            var xDoc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("Workflow",
                    new XAttribute("Id", workflow.WorkflowId),
                    new XAttribute("Version", workflow.Version),
                    new XElement("Name", workflow.Name),
                    new XElement("Description", workflow.Description ?? string.Empty),
                    new XElement("IsActive", workflow.IsActive),
                    new XElement("CronExpression", workflow.CronExpression ?? string.Empty),
                    new XElement("EnableSchedule", workflow.EnableSchedule),
                    new XElement("CreatedAt", workflow.CreatedAt.ToString("O")),
                    new XElement("UpdatedAt", workflow.UpdatedAt?.ToString("O") ?? string.Empty),

                    // 序列化节点
                    new XElement("Nodes",
                        workflow.Nodes.Select(node => new XElement("Node",
                            new XAttribute("Id", node.NodeId),
                            new XAttribute("Type", node.NodeType),
                            new XElement("Name", node.Name),
                            new XElement("Description", node.Description ?? string.Empty),
                            new XElement("Configuration", new XCData(node.Configuration ?? string.Empty)),
                            new XElement("Position",
                                new XAttribute("X", node.PositionX),
                                new XAttribute("Y", node.PositionY)
                            ),
                            new XElement("StyleConfig", new XCData(node.StyleConfig ?? string.Empty))
                        ))
                    ),

                    // 序列化边
                    new XElement("Edges",
                        workflow.Edges.Select(edge => new XElement("Edge",
                            new XAttribute("Id", edge.EdgeId),
                            new XAttribute("SourceNodeId", edge.SourceNodeId),
                            new XAttribute("TargetNodeId", edge.TargetNodeId),
                            new XElement("Label", edge.Label ?? string.Empty),
                            new XElement("Condition", edge.Condition ?? string.Empty),
                            new XElement("Priority", edge.Priority),
                            new XElement("StyleConfig", new XCData(edge.StyleConfig ?? string.Empty))
                        ))
                    )
                )
            );

            return Task.FromResult(xDoc.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize workflow {WorkflowId} to XML", workflow.WorkflowId);
            throw;
        }
    }

    public Task<Workflow> DeserializeFromXmlAsync(string xmlContent)
    {
        try
        {
            var xDoc = XDocument.Parse(xmlContent);
            var root = xDoc.Root ?? throw new InvalidOperationException("XML root element not found");

            var workflow = new Workflow
            {
                WorkflowId = Guid.Parse(root.Attribute("Id")?.Value ?? Guid.NewGuid().ToString()),
                Version = int.Parse(root.Attribute("Version")?.Value ?? "1"),
                Name = root.Element("Name")?.Value ?? string.Empty,
                Description = root.Element("Description")?.Value,
                IsActive = bool.Parse(root.Element("IsActive")?.Value ?? "true"),
                CronExpression = root.Element("CronExpression")?.Value,
                EnableSchedule = bool.Parse(root.Element("EnableSchedule")?.Value ?? "false"),
                XmlContent = xmlContent
            };

            // 解析创建和更新时间
            if (DateTime.TryParse(root.Element("CreatedAt")?.Value, out var createdAt))
            {
                workflow.CreatedAt = createdAt;
            }

            if (DateTime.TryParse(root.Element("UpdatedAt")?.Value, out var updatedAt))
            {
                workflow.UpdatedAt = updatedAt;
            }

            // 解析节点
            var nodesElement = root.Element("Nodes");
            if (nodesElement != null)
            {
                foreach (var nodeElement in nodesElement.Elements("Node"))
                {
                    var node = new WorkflowNode
                    {
                        NodeId = Guid.Parse(nodeElement.Attribute("Id")?.Value ?? Guid.NewGuid().ToString()),
                        WorkflowId = workflow.WorkflowId,
                        NodeType = nodeElement.Attribute("Type")?.Value ?? string.Empty,
                        Name = nodeElement.Element("Name")?.Value ?? string.Empty,
                        Description = nodeElement.Element("Description")?.Value,
                        Configuration = nodeElement.Element("Configuration")?.Value,
                        StyleConfig = nodeElement.Element("StyleConfig")?.Value
                    };

                    var positionElement = nodeElement.Element("Position");
                    if (positionElement != null)
                    {
                        node.PositionX = double.Parse(positionElement.Attribute("X")?.Value ?? "0");
                        node.PositionY = double.Parse(positionElement.Attribute("Y")?.Value ?? "0");
                    }

                    workflow.Nodes.Add(node);
                }
            }

            // 解析边
            var edgesElement = root.Element("Edges");
            if (edgesElement != null)
            {
                foreach (var edgeElement in edgesElement.Elements("Edge"))
                {
                    var edge = new WorkflowEdge
                    {
                        EdgeId = Guid.Parse(edgeElement.Attribute("Id")?.Value ?? Guid.NewGuid().ToString()),
                        WorkflowId = workflow.WorkflowId,
                        SourceNodeId = Guid.Parse(edgeElement.Attribute("SourceNodeId")?.Value ?? Guid.Empty.ToString()),
                        TargetNodeId = Guid.Parse(edgeElement.Attribute("TargetNodeId")?.Value ?? Guid.Empty.ToString()),
                        Label = edgeElement.Element("Label")?.Value,
                        Condition = edgeElement.Element("Condition")?.Value,
                        Priority = int.Parse(edgeElement.Element("Priority")?.Value ?? "0"),
                        StyleConfig = edgeElement.Element("StyleConfig")?.Value
                    };

                    workflow.Edges.Add(edge);
                }
            }

            return Task.FromResult(workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize workflow from XML");
            throw;
        }
    }

    public async Task<string> SaveToFileAsync(Workflow workflow, string? filePath = null)
    {
        try
        {
            // 如果没有提供文件路径，生成默认路径
            if (string.IsNullOrEmpty(filePath))
            {
                var fileName = $"{workflow.Name.Replace(" ", "_")}_{workflow.WorkflowId}.xml";
                filePath = Path.Combine(_workflowDirectory, fileName);
            }

            // 确保目录存在
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var xmlContent = await SerializeToXmlAsync(workflow);
            await File.WriteAllTextAsync(filePath, xmlContent);

            _logger.LogInformation("Workflow {WorkflowId} saved to file {FilePath}", workflow.WorkflowId, filePath);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save workflow {WorkflowId} to file", workflow.WorkflowId);
            throw;
        }
    }

    public async Task<Workflow> LoadFromFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Workflow file not found: {filePath}");
            }

            var xmlContent = await File.ReadAllTextAsync(filePath);
            var workflow = await DeserializeFromXmlAsync(xmlContent);
            workflow.XmlFilePath = filePath;

            _logger.LogInformation("Workflow loaded from file {FilePath}", filePath);

            return workflow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workflow from file {FilePath}", filePath);
            throw;
        }
    }

    public Task<(bool IsValid, string? ErrorMessage)> ValidateXmlAsync(string xmlContent)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(xmlContent))
            {
                return Task.FromResult((false, "XML content is empty"));
            }

            var xDoc = XDocument.Parse(xmlContent);
            var root = xDoc.Root;

            if (root == null || root.Name != "Workflow")
            {
                return Task.FromResult((false, "Invalid XML: root element must be 'Workflow'"));
            }

            // 验证必需字段
            if (string.IsNullOrEmpty(root.Element("Name")?.Value))
            {
                return Task.FromResult((false, "Invalid XML: 'Name' element is required"));
            }

            // 验证节点
            var nodesElement = root.Element("Nodes");
            if (nodesElement != null)
            {
                var nodes = nodesElement.Elements("Node").ToList();
                var nodeIds = new HashSet<Guid>();

                foreach (var node in nodes)
                {
                    var nodeIdStr = node.Attribute("Id")?.Value;
                    if (string.IsNullOrEmpty(nodeIdStr) || !Guid.TryParse(nodeIdStr, out var nodeId))
                    {
                        return Task.FromResult((false, "Invalid XML: node Id is invalid"));
                    }

                    if (!nodeIds.Add(nodeId))
                    {
                        return Task.FromResult((false, $"Invalid XML: duplicate node Id '{nodeId}'"));
                    }
                }

                // 验证边
                var edgesElement = root.Element("Edges");
                if (edgesElement != null)
                {
                    foreach (var edge in edgesElement.Elements("Edge"))
                    {
                        var sourceIdStr = edge.Attribute("SourceNodeId")?.Value;
                        var targetIdStr = edge.Attribute("TargetNodeId")?.Value;

                        if (!Guid.TryParse(sourceIdStr, out var sourceId) ||
                            !Guid.TryParse(targetIdStr, out var targetId))
                        {
                            return Task.FromResult((false, "Invalid XML: edge node Ids are invalid"));
                        }

                        if (!nodeIds.Contains(sourceId))
                        {
                            return Task.FromResult((false, $"Invalid XML: source node '{sourceId}' not found"));
                        }

                        if (!nodeIds.Contains(targetId))
                        {
                            return Task.FromResult((false, $"Invalid XML: target node '{targetId}' not found"));
                        }
                    }
                }
            }

            return Task.FromResult((true, (string?)null));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, $"XML parsing error: {ex.Message}"));
        }
    }
}
