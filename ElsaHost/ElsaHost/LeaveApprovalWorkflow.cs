using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace ElsaHost;

public class LeaveApprovalWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var leaveRequestId = builder.WithVariable<int>();
        var totalDays = builder.WithVariable<int>();
        var approvalPath = builder.WithVariable<string>();

        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Leave approval workflow started"),

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

                new WriteLine("Leave approval workflow input captured"),
                new End()
            }
        };
    }
}