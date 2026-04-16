using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using ElsaHost.Models;

namespace ElsaHost.Activities;

[Activity(
    Category = "Leave Approval",
    DisplayName = "Wait For HR Decision",
    Description = "Suspends the workflow until an HR decision is received."
)]
public class WaitForHrDecisionActivity : Activity
{
    [Input(Description = "Leave request ID")]
    public Input<int> LeaveRequestId { get; set; } = default!;

    [Output(Description = "HR action")]
    public Output<string> Action { get; set; } = default!;

    [Output(Description = "HR comment")]
    public Output<string?> Comment { get; set; } = default!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var leaveRequestId = context.Get(LeaveRequestId);

        context.CreateBookmark(new CreateBookmarkArgs
        {
            BookmarkName = nameof(WaitForHrDecisionActivity),
            Stimulus = new HrDecisionStimulus
            {
                LeaveRequestId = leaveRequestId
            },
            Callback = ResumeAsync,
            AutoBurn = true,
            AutoComplete = false
        });

        return ValueTask.CompletedTask;
    }

    private async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        var input = context.WorkflowInput;

        var action = input.TryGetValue("Action", out var actionValue)
            ? actionValue?.ToString() ?? string.Empty
            : string.Empty;

        var comment = input.TryGetValue("Comment", out var commentValue)
            ? commentValue?.ToString()
            : null;

        context.Set(Action, action);
        context.Set(Comment, comment);

        await context.CompleteActivityAsync();
    }
}