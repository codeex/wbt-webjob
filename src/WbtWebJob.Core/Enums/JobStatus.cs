namespace WbtWebJob.Core.Enums;

public enum JobStatus
{
    /// <summary>
    /// 待执行
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 执行中
    /// </summary>
    Running = 2,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 3,

    /// <summary>
    /// 失败
    /// </summary>
    Failed = 4,

    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled = 5
}
