using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WbtWebJob.Models;

public class WebJob
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid JobId { get; set; }

    [Required]
    [MaxLength(100)]
    public string JobType { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string BusinessId { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ScheduledAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Running, Completed, Failed

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    public string? JobParameters { get; set; } // JSON格式存储参数

    public string? HangfireJobId { get; set; } // Hangfire的Job ID

    // 导航属性
    public virtual ICollection<JobLog> JobLogs { get; set; } = new List<JobLog>();
}
