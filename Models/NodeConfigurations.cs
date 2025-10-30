namespace WbtWebJob.Models;

/// <summary>
/// 节点配置基类
/// </summary>
public abstract class NodeConfiguration
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 开始节点配置
/// </summary>
public class StartNodeConfiguration : NodeConfiguration
{
    /// <summary>
    /// 变量字典，供后续组件使用
    /// </summary>
    public Dictionary<string, object>? Variables { get; set; }
}

/// <summary>
/// 触发器节点配置（Cron定时）
/// </summary>
public class TriggerNodeConfiguration : NodeConfiguration
{
    /// <summary>
    /// Cron表达式
    /// </summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// Cron字段（辅助功能）
    /// </summary>
    public string? CronMinute { get; set; }
    public string? CronHour { get; set; }
    public string? CronDay { get; set; }
    public string? CronMonth { get; set; }
    public string? CronWeek { get; set; }
}

/// <summary>
/// 事件节点配置
/// </summary>
public class EventNodeConfiguration : NodeConfiguration
{
    /// <summary>
    /// 事件主题名
    /// </summary>
    public string EventTopic { get; set; } = string.Empty;
}

/// <summary>
/// HTTP授权节点配置
/// </summary>
public class HttpAuthNodeConfiguration : NodeConfiguration
{
    /// <summary>
    /// 授权URL（支持curl解析）
    /// </summary>
    public string AuthUrl { get; set; } = string.Empty;

    /// <summary>
    /// 授权方式（None, Basic, Bearer, ApiKey）
    /// </summary>
    public string AuthType { get; set; } = "None";

    /// <summary>
    /// Curl命令（可选）
    /// </summary>
    public string? CurlCommand { get; set; }

    /// <summary>
    /// HTTP方法
    /// </summary>
    public string HttpMethod { get; set; } = "POST";

    /// <summary>
    /// 请求头
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// 请求体
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// 断言表达式（支持 response[0].data[0].result==200）
    /// </summary>
    public string? AssertionExpression { get; set; }
}

/// <summary>
/// HTTP处理节点配置
/// </summary>
public class HttpActionNodeConfiguration : NodeConfiguration
{
    /// <summary>
    /// URL（支持curl解析）
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Curl命令（可选）
    /// </summary>
    public string? CurlCommand { get; set; }

    /// <summary>
    /// HTTP方法
    /// </summary>
    public string HttpMethod { get; set; } = "POST";

    /// <summary>
    /// 请求头
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// 请求体
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// 断言表达式（支持 response[0].data[0].result==200）
    /// </summary>
    public string? AssertionExpression { get; set; }
}

/// <summary>
/// 命令行节点配置
/// </summary>
public class CommandLineNodeConfiguration : NodeConfiguration
{
    /// <summary>
    /// 命令内容
    /// </summary>
    public string Command { get; set; } = string.Empty;
}

/// <summary>
/// 条件节点配置（如果）
/// </summary>
public class ConditionNodeConfiguration : NodeConfiguration
{
    /// <summary>
    /// 条件表达式
    /// </summary>
    public string ConditionExpression { get; set; } = string.Empty;
}

/// <summary>
/// 结束节点配置
/// </summary>
public class EndNodeConfiguration : NodeConfiguration
{
}
