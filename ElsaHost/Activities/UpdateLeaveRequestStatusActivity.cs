using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using ElsaHost.Models;
using ElsaHost.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ElsaHost.Activities;

[Activity(
    Category = "Leave Approval",
    DisplayName = "Update Leave Request Status",
    Description = "Updates leave request status in the business database."
)]
public class UpdateLeaveRequestStatusActivity : CodeActivity
{
    [Input(Description = "Leave request ID")]
    public Input<int> LeaveRequestId { get; set; } = default!;

    [Input(Description = "Status")]
    public Input<string> Status { get; set; } = default!;

    [Input(Description = "Current step")]
    public Input<string> CurrentStep { get; set; } = default!;

    [Input(Description = "Current approver ID")]
    public Input<string?> CurrentApproverId { get; set; } = default!;

    [Input(Description = "Current approver role")]
    public Input<string?> CurrentApproverRole { get; set; } = default!;

    [Input(Description = "Manager assigned time")]
    public Input<DateTime?> ManagerAssignedAt { get; set; } = default!;

    [Input(Description = "Manager due time")]
    public Input<DateTime?> ManagerDueAt { get; set; } = default!;

    [Input(Description = "HR assigned time")]
    public Input<DateTime?> HrAssignedAt { get; set; } = default!;

    [Input(Description = "HR due time")]
    public Input<DateTime?> HrDueAt { get; set; } = default!;

    [Input(Description = "Is overdue")]
    public Input<bool?> IsOverdue { get; set; } = default!;

    [Input(Description = "Last reminder time")]
    public Input<DateTime?> LastReminderAt { get; set; } = default!;

    [Input(Description = "Approved time")]
    public Input<DateTime?> ApprovedAt { get; set; } = default!;

    [Input(Description = "Rejected time")]
    public Input<DateTime?> RejectedAt { get; set; } = default!;

    [Input(Description = "Whether to add approval history")]
    public Input<bool> AddHistory { get; set; } = new(false);

    [Input(Description = "History approver role")]
    public Input<string?> HistoryApproverRole { get; set; } = default!;

    [Input(Description = "History approver ID")]
    public Input<string?> HistoryApproverId { get; set; } = default!;

    [Input(Description = "History approver name")]
    public Input<string?> HistoryApproverName { get; set; } = default!;

    [Input(Description = "History step name")]
    public Input<string?> HistoryStepName { get; set; } = default!;

    [Input(Description = "History action")]
    public Input<string?> HistoryAction { get; set; } = default!;

    [Input(Description = "History comment")]
    public Input<string?> HistoryComment { get; set; } = default!;

    [Input(Description = "History from status")]
    public Input<string?> HistoryFromStatus { get; set; } = default!;

    [Input(Description = "History to status")]
    public Input<string?> HistoryToStatus { get; set; } = default!;

    [Input(Description = "History from step")]
    public Input<string?> HistoryFromStep { get; set; } = default!;

    [Input(Description = "History to step")]
    public Input<string?> HistoryToStep { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var leaveRequestService = context.GetRequiredService<ILeaveRequestService>();

        var dto = new UpdateLeaveRequestStatusDto
        {
            LeaveRequestId = context.Get(LeaveRequestId),
            Status = context.Get(Status),
            CurrentStep = context.Get(CurrentStep),
            CurrentApproverId = context.Get(CurrentApproverId),
            CurrentApproverRole = context.Get(CurrentApproverRole),
            ManagerAssignedAt = context.Get(ManagerAssignedAt),
            ManagerDueAt = context.Get(ManagerDueAt),
            HrAssignedAt = context.Get(HrAssignedAt),
            HrDueAt = context.Get(HrDueAt),
            IsOverdue = context.Get(IsOverdue),
            LastReminderAt = context.Get(LastReminderAt),
            ApprovedAt = context.Get(ApprovedAt),
            RejectedAt = context.Get(RejectedAt),

            AddHistory = context.Get(AddHistory),
            HistoryApproverRole = context.Get(HistoryApproverRole),
            HistoryApproverId = context.Get(HistoryApproverId),
            HistoryApproverName = context.Get(HistoryApproverName),
            HistoryStepName = context.Get(HistoryStepName),
            HistoryAction = context.Get(HistoryAction),
            HistoryComment = context.Get(HistoryComment),
            HistoryFromStatus = context.Get(HistoryFromStatus),
            HistoryToStatus = context.Get(HistoryToStatus),
            HistoryFromStep = context.Get(HistoryFromStep),
            HistoryToStep = context.Get(HistoryToStep)
        };

        await leaveRequestService.UpdateStatusAsync(dto, context.CancellationToken);
    }
}