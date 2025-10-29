using WbtWebJob.Core.Enums;

namespace WbtWebJob.Core.DTOs;

public class JobResponse
{
    public string Id { get; set; } = string.Empty;
    public string BusinessId { get; set; } = string.Empty;
    public JobType JobType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public JobStatus Status { get; set; }
    public string? Configuration { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
