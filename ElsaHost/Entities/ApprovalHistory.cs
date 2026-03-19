namespace ElsaHost.Entities;

public class ApprovalHistory
{
    public int Id { get; set; }
    public int LeaveRequestId { get; set; }

    public string ApproverRole { get; set; } = default!;
    public string StepName { get; set; } = default!;
    public string Action { get; set; } = default!;
    public string? Comment { get; set; }

    public DateTime ActionAt { get; set; } = DateTime.UtcNow;
}