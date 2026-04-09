namespace ElsaHost.Entities;

public class LeaveRequest
{
    public int Id { get; set; }
    public string EmployeeId { get; set; } = default!;
    public string EmployeeName { get; set; } = default!;

    public string? ManagerId { get; set; }
    public string? HrId { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = default!;
    public int TotalDays { get; set; }

    public string Status { get; set; } = "PendingManager";
    public string CurrentStep { get; set; } = "ManagerApproval";

    public string? CurrentApproverId { get; set; }
    public string? CurrentApproverRole { get; set; }

    public DateTime? ManagerAssignedAt { get; set; }
    public DateTime? ManagerDueAt { get; set; }

    public DateTime? HrAssignedAt { get; set; }
    public DateTime? HrDueAt { get; set; }

    public bool IsOverdue { get; set; } = false;
    public DateTime? LastReminderAt { get; set; }

    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }

    public string? WorkflowInstanceId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}