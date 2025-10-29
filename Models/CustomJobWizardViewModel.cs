using System.ComponentModel.DataAnnotations;

namespace WbtWebJob.Models;

/// <summary>
/// 定制任务向导视图模型
/// </summary>
public class CustomJobWizardViewModel
{
    // 当前步骤 (1-3)
    public int CurrentStep { get; set; } = 1;

    // 步骤1: 基本信息
    [Required(ErrorMessage = "任务类型不能为空")]
    [MaxLength(100)]
    public string JobType { get; set; } = string.Empty;

    [Required(ErrorMessage = "任务名称不能为空")]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    // 步骤2: HTTP授权（可选）
    [MaxLength(50)]
    public string? AuthType { get; set; } // None, Basic, Bearer, ApiKey

    public string? AuthConfig { get; set; } // JSON格式

    // 步骤3: HTTP请求
    public string? CurlCommand { get; set; } // 用户粘贴的curl命令

    [Required(ErrorMessage = "HTTP URL不能为空")]
    [MaxLength(500)]
    public string HttpUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "HTTP方法不能为空")]
    [MaxLength(20)]
    public string HttpMethod { get; set; } = "POST";

    public string? Headers { get; set; } // JSON格式

    public string? RequestBody { get; set; } // 请求体内容

    public string? DefaultParameters { get; set; } // JSON格式

    [MaxLength(1000)]
    public string? AssertionExpression { get; set; } // 断言表达式

    // 编辑时使用
    public Guid? CustomJobId { get; set; }

    /// <summary>
    /// 转换为CustomJob实体
    /// </summary>
    public CustomJob ToCustomJob()
    {
        var customJob = new CustomJob
        {
            JobType = this.JobType,
            Name = this.Name,
            Description = this.Description,
            IsActive = this.IsActive,
            AuthType = this.AuthType,
            AuthConfig = this.AuthConfig,
            HttpUrl = this.HttpUrl,
            HttpMethod = this.HttpMethod,
            Headers = this.Headers,
            RequestBody = this.RequestBody,
            DefaultParameters = this.DefaultParameters,
            AssertionExpression = this.AssertionExpression,
            CurlCommand = this.CurlCommand
        };

        if (CustomJobId.HasValue)
        {
            customJob.CustomJobId = CustomJobId.Value;
        }

        return customJob;
    }

    /// <summary>
    /// 从CustomJob实体加载数据
    /// </summary>
    public static CustomJobWizardViewModel FromCustomJob(CustomJob customJob)
    {
        return new CustomJobWizardViewModel
        {
            CustomJobId = customJob.CustomJobId,
            JobType = customJob.JobType,
            Name = customJob.Name,
            Description = customJob.Description,
            IsActive = customJob.IsActive,
            AuthType = customJob.AuthType,
            AuthConfig = customJob.AuthConfig,
            HttpUrl = customJob.HttpUrl,
            HttpMethod = customJob.HttpMethod,
            Headers = customJob.Headers,
            RequestBody = customJob.RequestBody,
            DefaultParameters = customJob.DefaultParameters,
            AssertionExpression = customJob.AssertionExpression,
            CurlCommand = customJob.CurlCommand
        };
    }
}
