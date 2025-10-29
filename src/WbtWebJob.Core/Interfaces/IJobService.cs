using WbtWebJob.Core.DTOs;
using WbtWebJob.Core.Models;

namespace WbtWebJob.Core.Interfaces;

public interface IJobService
{
    /// <summary>
    /// 创建Job
    /// </summary>
    Task<JobResponse> CreateJobAsync(CreateJobRequest request);

    /// <summary>
    /// 根据JobId获取Job信息
    /// </summary>
    Task<JobResponse?> GetJobByIdAsync(string jobId);

    /// <summary>
    /// 根据业务ID获取Job信息
    /// </summary>
    Task<JobResponse?> GetJobByBusinessIdAsync(string businessId);

    /// <summary>
    /// 获取Job日志
    /// </summary>
    Task<List<JobLogResponse>> GetJobLogsAsync(string jobId);

    /// <summary>
    /// 执行Job
    /// </summary>
    Task ExecuteJobAsync(string jobId);

    /// <summary>
    /// 取消Job
    /// </summary>
    Task CancelJobAsync(string jobId);
}
