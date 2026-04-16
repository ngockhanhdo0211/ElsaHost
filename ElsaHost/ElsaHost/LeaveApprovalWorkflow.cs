using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using ElsaHost.Activities;
using ElsaHost.Constants;

namespace ElsaHost;

public class LeaveApprovalWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var leaveRequestId = builder.WithVariable<int>();
        var totalDays = builder.WithVariable<int>();
        var approvalPath = builder.WithVariable<string>();
        var managerAction = builder.WithVariable<string>();
        var managerComment = builder.WithVariable<string?>();
        var hrAction = builder.WithVariable<string>();
        var hrComment = builder.WithVariable<string?>();
        var managerDecisionCompleted = builder.WithVariable<bool>();
        var hrDecisionCompleted = builder.WithVariable<bool>();

        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Leave approval workflow started"),

                new SetVariable
                {
                    Variable = managerDecisionCompleted,
                    Value = new(_ => false)
                },

                new SetVariable
                {
                    Variable = hrDecisionCompleted,
                    Value = new(_ => false)
                },

                new SetVariable
                {
                    Variable = leaveRequestId,
                    Value = new(context => Convert.ToInt32(context.GetInput("LeaveRequestId") ?? 0))
                },

                new SetVariable
                {
                    Variable = totalDays,
                    Value = new(context => Convert.ToInt32(context.GetInput("TotalDays") ?? 0))
                },

                new SetVariable
                {
                    Variable = approvalPath,
                    Value = new(context =>
                    {
                        var days = totalDays.Get(context);
                        return days <= 2 ? "ManagerOnly" : "ManagerThenHr";
                    })
                },

                new SetVariable
                {
                    Variable = managerAction,
                    Value = new(_ => string.Empty)
                },

                new SetVariable
                {
                    Variable = managerComment,
                    Value = new(_ => null)
                },

                new SetVariable
                {
                    Variable = hrAction,
                    Value = new(_ => string.Empty)
                },

                new SetVariable
                {
                    Variable = hrComment,
                    Value = new(_ => null)
                },

                new WriteLine("Workflow inputs captured"),
                new WriteLine(context => $"LeaveRequestId = {leaveRequestId.Get(context)}"),
                new WriteLine(context => $"TotalDays = {totalDays.Get(context)}"),
                new WriteLine(context => $"ApprovalPath = {approvalPath.Get(context)}"),

                new UpdateLeaveRequestStatusActivity
                {
                    LeaveRequestId = new(leaveRequestId),
                    Status = new(LeaveStatuses.PendingManager),
                    CurrentStep = new(LeaveSteps.ManagerApproval),
                    CurrentApproverRole = new("Manager"),
                    ManagerAssignedAt = new(_ => DateTime.UtcNow),
                    ManagerDueAt = new(_ => DateTime.UtcNow.AddMinutes(1440)),
                    IsOverdue = new(false),
                    AddHistory = new(false)
                },

                new WriteLine("Manager stage initialized in database"),

                new WaitForManagerDecisionActivity
                {
                    LeaveRequestId = new(leaveRequestId),
                    Action = new(managerAction),
                    Comment = new(managerComment)
                },

                new SetVariable
                {
                    Variable = managerDecisionCompleted,
                    Value = new(_ => true)
                },

                new WriteLine(context => $"ManagerAction = {managerAction.Get(context)}"),
                new WriteLine(context => $"ManagerComment = {managerComment.Get(context)}"),

                new If(context =>
                    string.Equals(managerAction.Get(context), "Reject", StringComparison.OrdinalIgnoreCase))
                {
                    Then = new Sequence
                    {
                        Activities =
                        {
                            new UpdateLeaveRequestStatusActivity
                            {
                                LeaveRequestId = new(leaveRequestId),
                                Status = new(LeaveStatuses.Rejected),
                                CurrentStep = new(LeaveSteps.Completed),
                                CurrentApproverId = new((string?)null),
                                CurrentApproverRole = new((string?)null),
                                RejectedAt = new(_ => DateTime.UtcNow),
                                IsOverdue = new(false),
                                AddHistory = new(true),
                                HistoryApproverRole = new("Manager"),
                                HistoryStepName = new(LeaveSteps.ManagerApproval),
                                HistoryAction = new(ApprovalHistoryActions.ManagerRejected),
                                HistoryComment = new(managerComment),
                                HistoryToStatus = new(LeaveStatuses.Rejected),
                                HistoryToStep = new(LeaveSteps.Completed)
                            },
                            new WriteLine("Manager rejected. Workflow completed.")
                        }
                    },
                    Else = new Sequence
                    {
                        Activities =
                        {
                            new If(context => totalDays.Get(context) <= 2)
                            {
                                Then = new Sequence
                                {
                                    Activities =
                                    {
                                        new UpdateLeaveRequestStatusActivity
                                        {
                                            LeaveRequestId = new(leaveRequestId),
                                            Status = new(LeaveStatuses.Approved),
                                            CurrentStep = new(LeaveSteps.Completed),
                                            CurrentApproverId = new((string?)null),
                                            CurrentApproverRole = new((string?)null),
                                            ApprovedAt = new(_ => DateTime.UtcNow),
                                            IsOverdue = new(false),
                                            AddHistory = new(true),
                                            HistoryApproverRole = new("Manager"),
                                            HistoryStepName = new(LeaveSteps.ManagerApproval),
                                            HistoryAction = new(ApprovalHistoryActions.ManagerApproved),
                                            HistoryComment = new(managerComment),
                                            HistoryToStatus = new(LeaveStatuses.Approved),
                                            HistoryToStep = new(LeaveSteps.Completed)
                                        },
                                        new WriteLine("Manager approved. TotalDays <= 2. Workflow completed.")
                                    }
                                },
                                Else = new Sequence
                                {
                                    Activities =
                                    {
                                        new UpdateLeaveRequestStatusActivity
                                        {
                                            LeaveRequestId = new(leaveRequestId),
                                            Status = new(LeaveStatuses.PendingHR),
                                            CurrentStep = new(LeaveSteps.HrApproval),
                                            CurrentApproverRole = new("HR"),
                                            HrAssignedAt = new(_ => DateTime.UtcNow),
                                            HrDueAt = new(_ => DateTime.UtcNow.AddMinutes(1440)),
                                            IsOverdue = new(false),
                                            AddHistory = new(true),
                                            HistoryApproverRole = new("Manager"),
                                            HistoryStepName = new(LeaveSteps.ManagerApproval),
                                            HistoryAction = new(ApprovalHistoryActions.ManagerApproved),
                                            HistoryComment = new(managerComment),
                                            HistoryToStatus = new(LeaveStatuses.PendingHR),
                                            HistoryToStep = new(LeaveSteps.HrApproval)
                                        },

                                        new WriteLine("Manager approved. Forwarded to HR."),

                                        new WaitForHrDecisionActivity
                                        {
                                            LeaveRequestId = new(leaveRequestId),
                                            Action = new(hrAction),
                                            Comment = new(hrComment)
                                        },

                                        new SetVariable
                                        {
                                            Variable = hrDecisionCompleted,
                                            Value = new(_ => true)
                                        },

                                        new WriteLine(context => $"HrAction = {hrAction.Get(context)}"),
                                        new WriteLine(context => $"HrComment = {hrComment.Get(context)}"),

                                        new If(context =>
                                            string.Equals(hrAction.Get(context), "Reject", StringComparison.OrdinalIgnoreCase))
                                        {
                                            Then = new Sequence
                                            {
                                                Activities =
                                                {
                                                    new UpdateLeaveRequestStatusActivity
                                                    {
                                                        LeaveRequestId = new(leaveRequestId),
                                                        Status = new(LeaveStatuses.Rejected),
                                                        CurrentStep = new(LeaveSteps.Completed),
                                                        CurrentApproverId = new((string?)null),
                                                        CurrentApproverRole = new((string?)null),
                                                        RejectedAt = new(_ => DateTime.UtcNow),
                                                        IsOverdue = new(false),
                                                        AddHistory = new(true),
                                                        HistoryApproverRole = new("HR"),
                                                        HistoryStepName = new(LeaveSteps.HrApproval),
                                                        HistoryAction = new(ApprovalHistoryActions.HrRejected),
                                                        HistoryComment = new(hrComment),
                                                        HistoryToStatus = new(LeaveStatuses.Rejected),
                                                        HistoryToStep = new(LeaveSteps.Completed)
                                                    },
                                                    new WriteLine("HR rejected. Workflow completed.")
                                                }
                                            },
                                            Else = new Sequence
                                            {
                                                Activities =
                                                {
                                                    new UpdateLeaveRequestStatusActivity
                                                    {
                                                        LeaveRequestId = new(leaveRequestId),
                                                        Status = new(LeaveStatuses.Approved),
                                                        CurrentStep = new(LeaveSteps.Completed),
                                                        CurrentApproverId = new((string?)null),
                                                        CurrentApproverRole = new((string?)null),
                                                        ApprovedAt = new(_ => DateTime.UtcNow),
                                                        IsOverdue = new(false),
                                                        AddHistory = new(true),
                                                        HistoryApproverRole = new("HR"),
                                                        HistoryStepName = new(LeaveSteps.HrApproval),
                                                        HistoryAction = new(ApprovalHistoryActions.HrApproved),
                                                        HistoryComment = new(hrComment),
                                                        HistoryToStatus = new(LeaveStatuses.Approved),
                                                        HistoryToStep = new(LeaveSteps.Completed)
                                                    },
                                                    new WriteLine("HR approved. Workflow completed.")
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },

                new End()
            }
        };
    }
}