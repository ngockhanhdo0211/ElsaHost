using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using ElsaHost.Models;

namespace ElsaHost.Activities;

[Activity(
    Category = "Leave Approval",
    DisplayName = "Wait For Manager Timeout",
    Description = "Suspends until manager timeout occurs."
)]
public class WaitForManagerTimeoutActivity : Activity
{
    [Input(Description = "Leave request ID")]
    public Input<int> LeaveRequestId { get; set; } = default!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var leaveRequestId = context.Get(LeaveRequestId);

        context.CreateBookmark(new CreateBookmarkArgs
        {
            BookmarkName = nameof(WaitForManagerTimeoutActivity),
            Stimulus = new ManagerDecisionStimulus
            {
                LeaveRequestId = leaveRequestId
            },
            AutoBurn = true,
            AutoComplete = false
        });

        return ValueTask.CompletedTask;
    }
}