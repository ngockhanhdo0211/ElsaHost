namespace ElsaHost.Entities;

public class LeaveRequest
{
    public int Id { get; set; }
    public string EmployeeName { get; set; } = default!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = default!;
    public int TotalDays { get; set; }

    public string Status { get; set; } = "PendingManager";
    public string CurrentStep { get; set; } = "ManagerApproval";
    public string? WorkflowInstanceId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}