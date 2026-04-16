using ElsaHost.Constants;
using ElsaHost.Data;
using ElsaHost.Entities;
using ElsaHost.Models;
using Microsoft.EntityFrameworkCore;

namespace ElsaHost.Services;

public class LeaveRequestService : ILeaveRequestService
{
    private readonly AppDbContext _dbContext;

    public LeaveRequestService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LeaveRequest> CreateAsync(CreateLeaveRequestDto dto, CancellationToken cancellationToken = default)
    {
        var totalDays = (dto.EndDate.Date - dto.StartDate.Date).Days + 1;

        if (string.IsNullOrWhiteSpace(dto.EmployeeName))
            throw new InvalidOperationException("EmployeeName is required.");

        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Reason is required.");

        if (dto.EndDate.Date < dto.StartDate.Date)
            throw new InvalidOperationException("EndDate must be greater than or equal to StartDate.");

        var now = DateTime.UtcNow;

        var request = new LeaveRequest
        {
            EmployeeId = dto.EmployeeName.Trim(),
            EmployeeName = dto.EmployeeName.Trim(),
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date,
            Reason = dto.Reason.Trim(),
            TotalDays = totalDays,
            Status = LeaveStatuses.PendingManager,
            CurrentStep = LeaveSteps.ManagerApproval,
            CurrentApproverRole = "Manager",
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.LeaveRequests.Add(request);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.ApprovalHistories.Add(new ApprovalHistory
        {
            LeaveRequestId = request.Id,
            ApproverRole = "Employee",
            ApproverId = request.EmployeeId,
            ApproverName = request.EmployeeName,
            StepName = LeaveSteps.ManagerApproval,
            Action = ApprovalHistoryActions.Submitted,
            Comment = "Leave request created",
            FromStatus = null,
            ToStatus = LeaveStatuses.PendingManager,
            FromStep = null,
            ToStep = LeaveSteps.ManagerApproval,
            ActionAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return request;
    }

    public async Task<LeaveRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaveRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<LeaveRequest>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaveRequests
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ApprovalHistory>> GetHistoryAsync(int leaveRequestId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ApprovalHistories
            .Where(x => x.LeaveRequestId == leaveRequestId)
            .OrderBy(x => x.ActionAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Success, string Message, LeaveRequest? Request)> ValidateManagerDecisionAsync(
        int id,
        DecisionDto dto,
        CancellationToken cancellationToken = default)
    {
        var request = await _dbContext.LeaveRequests
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (request == null)
            return (false, "Không tìm thấy đơn nghỉ phép.", null);

        if (!string.Equals(request.Status, LeaveStatuses.PendingManager, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.Status, LeaveStatuses.ManagerOverdue, StringComparison.OrdinalIgnoreCase))
            return (false, "Đơn hiện không ở bước Manager approval.", null);

        if (string.IsNullOrWhiteSpace(dto.Action))
            return (false, "Action là bắt buộc. Dùng Approve hoặc Reject.", null);

        var action = dto.Action.Trim();

        if (!string.Equals(action, "Approve", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(action, "Reject", StringComparison.OrdinalIgnoreCase))
            return (false, "Action không hợp lệ. Dùng Approve hoặc Reject.", null);

        return (true, "Manager decision hợp lệ.", request);
    }

    public async Task<(bool Success, string Message, LeaveRequest? Request)> ValidateHrDecisionAsync(
        int id,
        DecisionDto dto,
        CancellationToken cancellationToken = default)
    {
        var request = await _dbContext.LeaveRequests
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (request == null)
            return (false, "Không tìm thấy đơn nghỉ phép.", null);

        if (!string.Equals(request.Status, LeaveStatuses.PendingHR, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.Status, LeaveStatuses.HROverdue, StringComparison.OrdinalIgnoreCase))
            return (false, "Đơn hiện không ở bước HR approval.", null);

        if (string.IsNullOrWhiteSpace(dto.Action))
            return (false, "Action là bắt buộc. Dùng Approve hoặc Reject.", null);

        var action = dto.Action.Trim();

        if (!string.Equals(action, "Approve", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(action, "Reject", StringComparison.OrdinalIgnoreCase))
            return (false, "Action không hợp lệ. Dùng Approve hoặc Reject.", null);

        return (true, "HR decision hợp lệ.", request);
    }

    public async Task<LeaveRequest> UpdateStatusAsync(UpdateLeaveRequestStatusDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.LeaveRequestId <= 0)
            throw new InvalidOperationException("LeaveRequestId is invalid.");

        if (string.IsNullOrWhiteSpace(dto.Status))
            throw new InvalidOperationException("Status is required.");

        if (string.IsNullOrWhiteSpace(dto.CurrentStep))
            throw new InvalidOperationException("CurrentStep is required.");

        var request = await _dbContext.LeaveRequests
            .FirstOrDefaultAsync(x => x.Id == dto.LeaveRequestId, cancellationToken);

        if (request == null)
            throw new InvalidOperationException($"Không tìm thấy đơn nghỉ phép với id = {dto.LeaveRequestId}.");

        var oldStatus = request.Status;
        var oldStep = request.CurrentStep;

        request.Status = dto.Status.Trim();
        request.CurrentStep = dto.CurrentStep.Trim();

        request.CurrentApproverId = dto.CurrentApproverId;
        request.CurrentApproverRole = dto.CurrentApproverRole;

        request.ManagerAssignedAt = dto.ManagerAssignedAt ?? request.ManagerAssignedAt;
        request.ManagerDueAt = dto.ManagerDueAt ?? request.ManagerDueAt;

        request.HrAssignedAt = dto.HrAssignedAt ?? request.HrAssignedAt;
        request.HrDueAt = dto.HrDueAt ?? request.HrDueAt;

        if (dto.IsOverdue.HasValue)
            request.IsOverdue = dto.IsOverdue.Value;

        request.LastReminderAt = dto.LastReminderAt ?? request.LastReminderAt;
        request.ApprovedAt = dto.ApprovedAt ?? request.ApprovedAt;
        request.RejectedAt = dto.RejectedAt ?? request.RejectedAt;
        request.UpdatedAt = DateTime.UtcNow;

        if (dto.AddHistory)
        {
            if (string.IsNullOrWhiteSpace(dto.HistoryApproverRole))
                throw new InvalidOperationException("HistoryApproverRole is required when AddHistory = true.");

            if (string.IsNullOrWhiteSpace(dto.HistoryStepName))
                throw new InvalidOperationException("HistoryStepName is required when AddHistory = true.");

            if (string.IsNullOrWhiteSpace(dto.HistoryAction))
                throw new InvalidOperationException("HistoryAction is required when AddHistory = true.");

            _dbContext.ApprovalHistories.Add(new ApprovalHistory
            {
                LeaveRequestId = request.Id,
                ApproverRole = dto.HistoryApproverRole.Trim(),
                ApproverId = dto.HistoryApproverId,
                ApproverName = dto.HistoryApproverName,
                StepName = dto.HistoryStepName.Trim(),
                Action = dto.HistoryAction.Trim(),
                Comment = dto.HistoryComment,
                FromStatus = dto.HistoryFromStatus ?? oldStatus,
                ToStatus = dto.HistoryToStatus ?? request.Status,
                FromStep = dto.HistoryFromStep ?? oldStep,
                ToStep = dto.HistoryToStep ?? request.CurrentStep,
                ActionAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return request;
    }
}