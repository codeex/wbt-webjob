namespace WbtWebJob.Core.DTOs;

public class JobLogResponse
{
    public string Id { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? StepName { get; set; }
    public DateTime CreatedAt { get; set; }
}
