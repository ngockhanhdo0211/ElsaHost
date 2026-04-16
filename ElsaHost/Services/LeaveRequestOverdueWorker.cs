using ElsaHost.Constants;
using ElsaHost.Data;
using ElsaHost.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElsaHost.Services;

public class LeaveRequestOverdueWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LeaveRequestOverdueWorker> _logger;

    public LeaveRequestOverdueWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<LeaveRequestOverdueWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LeaveRequestOverdueWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var now = DateTime.UtcNow;

                await MarkManagerOverdueAsync(dbContext, now, stoppingToken);
                await MarkHrOverdueAsync(dbContext, now, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing overdue leave requests.");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    private static async Task MarkManagerOverdueAsync(
        AppDbContext dbContext,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var overdueRequests = await dbContext.LeaveRequests
            .Where(x =>
                x.Status == LeaveStatuses.PendingManager &&
                x.ManagerDueAt != null &&
                x.ManagerDueAt <= now &&
                !x.IsOverdue)
            .ToListAsync(cancellationToken);

        if (overdueRequests.Count == 0)
            return;

        foreach (var request in overdueRequests)
        {
            var oldStatus = request.Status;
            var oldStep = request.CurrentStep;

            request.Status = LeaveStatuses.ManagerOverdue;
            request.CurrentStep = LeaveSteps.ManagerApproval;
            request.CurrentApproverRole = "Manager";
            request.IsOverdue = true;
            request.LastReminderAt = now;
            request.UpdatedAt = now;

            dbContext.ApprovalHistories.Add(new ApprovalHistory
            {
                LeaveRequestId = request.Id,
                ApproverRole = "Manager",
                ApproverId = request.ManagerId,
                ApproverName = null,
                StepName = LeaveSteps.ManagerApproval,
                Action = ApprovalHistoryActions.ManagerTimeout,
                Comment = "Manager did not respond before due time.",
                FromStatus = oldStatus,
                ToStatus = LeaveStatuses.ManagerOverdue,
                FromStep = oldStep,
                ToStep = LeaveSteps.ManagerApproval,
                ActionAt = now
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task MarkHrOverdueAsync(
        AppDbContext dbContext,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var overdueRequests = await dbContext.LeaveRequests
            .Where(x =>
                x.Status == LeaveStatuses.PendingHR &&
                x.HrDueAt != null &&
                x.HrDueAt <= now &&
                !x.IsOverdue)
            .ToListAsync(cancellationToken);

        if (overdueRequests.Count == 0)
            return;

        foreach (var request in overdueRequests)
        {
            var oldStatus = request.Status;
            var oldStep = request.CurrentStep;

            request.Status = LeaveStatuses.HROverdue;
            request.CurrentStep = LeaveSteps.HrApproval;
            request.CurrentApproverRole = "HR";
            request.IsOverdue = true;
            request.LastReminderAt = now;
            request.UpdatedAt = now;

            dbContext.ApprovalHistories.Add(new ApprovalHistory
            {
                LeaveRequestId = request.Id,
                ApproverRole = "HR",
                ApproverId = request.HrId,
                ApproverName = null,
                StepName = LeaveSteps.HrApproval,
                Action = ApprovalHistoryActions.HrTimeout,
                Comment = "HR did not respond before due time.",
                FromStatus = oldStatus,
                ToStatus = LeaveStatuses.HROverdue,
                FromStep = oldStep,
                ToStep = LeaveSteps.HrApproval,
                ActionAt = now
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}