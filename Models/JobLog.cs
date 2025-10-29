using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WbtWebJob.Models;

public class JobLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid LogId { get; set; }

    [Required]
    public Guid JobId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Step { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Level { get; set; } = "Info"; // Info, Warning, Error, Debug

    public string? Message { get; set; }

    public string? Details { get; set; } // JSON格式存储详细信息

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 导航属性
    [ForeignKey("JobId")]
    public virtual WebJob? WebJob { get; set; }
}
