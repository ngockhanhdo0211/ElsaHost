namespace ElsaHost.Entities;

public class ApprovalHistory
{
    public int Id { get; set; }
    public int LeaveRequestId { get; set; }

    public string ApproverRole { get; set; } = default!;
    public string? ApproverId { get; set; }
    public string? ApproverName { get; set; }

    public string StepName { get; set; } = default!;
    public string Action { get; set; } = default!;
    public string? Comment { get; set; }

    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }

    public string? FromStep { get; set; }
    public string? ToStep { get; set; }

    public DateTime ActionAt { get; set; } = DateTime.UtcNow;
}