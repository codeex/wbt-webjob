namespace WbtWebJob.Models;

/// <summary>
/// 工作流节点类型
/// </summary>
public enum WorkflowNodeType
{
    // 操作组（不是实际节点，只是工具）
    Select,      // 选择工具
    Connect,     // 连接工具

    // 输入组（只能连出，不能连入）
    Start,       // 开始节点
    Trigger,     // 触发器（Cron定时）
    Event,       // 事件触发

    // 处理组（可以连入连出）
    HttpAuth,    // HTTP授权
    HttpAction,  // HTTP处理
    CommandLine, // 命令行
    Condition,   // 如果条件（有两个输出：真/假）

    // 终止组（只能连入，不能连出）
    End          // 结束节点
}

/// <summary>
/// 节点类别
/// </summary>
public enum WorkflowNodeCategory
{
    Action,      // 操作组
    Input,       // 输入组
    Process,     // 处理组
    Terminate    // 终止组
}
