using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WbtWebJob.Models;

public class CustomJob
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid CustomJobId { get; set; }

    [Required]
    [MaxLength(100)]
    public string JobType { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(500)]
    public string HttpUrl { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string HttpMethod { get; set; } = "POST"; // GET, POST, PUT, DELETE

    [MaxLength(50)]
    public string? AuthType { get; set; } // None, Basic, Bearer, ApiKey

    public string? AuthConfig { get; set; } // JSON格式存储授权配置

    public string? Headers { get; set; } // JSON格式存储HTTP Headers

    public string? DefaultParameters { get; set; } // JSON格式存储默认参数

    public string? RequestBody { get; set; } // HTTP请求体内容

    public string? CurlCommand { get; set; } // 原始curl命令（可选，用于记录）

    [MaxLength(1000)]
    public string? AssertionExpression { get; set; } // 断言表达式，如: response[0].data[0].result==200

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
