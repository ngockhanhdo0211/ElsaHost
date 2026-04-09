namespace ElsaHost.Models;

public class UpdateLeaveRequestStatusDto
{
    public int LeaveRequestId { get; set; }

    public string Status { get; set; } = default!;
    public string CurrentStep { get; set; } = default!;

    public string? CurrentApproverId { get; set; }
    public string? CurrentApproverRole { get; set; }

    public DateTime? ManagerAssignedAt { get; set; }
    public DateTime? ManagerDueAt { get; set; }

    public DateTime? HrAssignedAt { get; set; }
    public DateTime? HrDueAt { get; set; }

    public bool? IsOverdue { get; set; }
    public DateTime? LastReminderAt { get; set; }

    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }

    public bool AddHistory { get; set; } = false;
    public string? HistoryApproverRole { get; set; }
    public string? HistoryStepName { get; set; }
    public string? HistoryAction { get; set; }
    public string? HistoryComment { get; set; }
}