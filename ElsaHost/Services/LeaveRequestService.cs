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

        var request = new LeaveRequest
        {
            EmployeeName = dto.EmployeeName.Trim(),
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date,
            Reason = dto.Reason.Trim(),
            TotalDays = totalDays,
            Status = LeaveStatuses.PendingManager,
            CurrentStep = LeaveSteps.ManagerReview,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.LeaveRequests.Add(request);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.ApprovalHistories.Add(new ApprovalHistory
        {
            LeaveRequestId = request.Id,
            ApproverRole = "Employee",
            StepName = LeaveSteps.ManagerReview,
            Action = "Submitted",
            Comment = "Leave request created",
            ActionAt = DateTime.UtcNow
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

    public async Task<(bool Success, string Message)> ManagerDecisionAsync(int id, DecisionDto dto, CancellationToken cancellationToken = default)
    {
        var request = await _dbContext.LeaveRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (request == null)
            return (false, "Không tìm thấy đơn nghỉ phép.");

        if (!string.Equals(request.Status, LeaveStatuses.PendingManager, StringComparison.OrdinalIgnoreCase))
            return (false, "Đơn hiện không ở bước Manager review.");

        if (string.IsNullOrWhiteSpace(dto.Action))
            return (false, "Action là bắt buộc. Dùng Approve hoặc Reject.");

        var action = dto.Action.Trim();

        if (!string.Equals(action, "Approve", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(action, "Reject", StringComparison.OrdinalIgnoreCase))
            return (false, "Action không hợp lệ. Dùng Approve hoặc Reject.");

        var isApproved = string.Equals(action, "Approve", StringComparison.OrdinalIgnoreCase);

        if (!isApproved)
        {
            request.Status = LeaveStatuses.Rejected;
            request.CurrentStep = LeaveSteps.Completed;
        }
        else
        {
            if (request.TotalDays <= 2)
            {
                request.Status = LeaveStatuses.Approved;
                request.CurrentStep = LeaveSteps.Completed;
            }
            else
            {
                request.Status = LeaveStatuses.PendingHr;
                request.CurrentStep = LeaveSteps.HrReview;
            }
        }

        request.UpdatedAt = DateTime.UtcNow;

        _dbContext.ApprovalHistories.Add(new ApprovalHistory
        {
            LeaveRequestId = request.Id,
            ApproverRole = "Manager",
            StepName = LeaveSteps.ManagerReview,
            Action = isApproved ? "Approved" : "Rejected",
            Comment = dto.Comment,
            ActionAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (true, "Manager xử lý thành công.");
    }

    public async Task<(bool Success, string Message)> HrDecisionAsync(int id, DecisionDto dto, CancellationToken cancellationToken = default)
    {
        var request = await _dbContext.LeaveRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (request == null)
            return (false, "Không tìm thấy đơn nghỉ phép.");

        if (!string.Equals(request.Status, LeaveStatuses.PendingHr, StringComparison.OrdinalIgnoreCase))
            return (false, "Đơn hiện không ở bước HR review.");

        if (string.IsNullOrWhiteSpace(dto.Action))
            return (false, "Action là bắt buộc. Dùng Approve hoặc Reject.");

        var action = dto.Action.Trim();

        if (!string.Equals(action, "Approve", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(action, "Reject", StringComparison.OrdinalIgnoreCase))
            return (false, "Action không hợp lệ. Dùng Approve hoặc Reject.");

        var isApproved = string.Equals(action, "Approve", StringComparison.OrdinalIgnoreCase);

        request.Status = isApproved ? LeaveStatuses.Approved : LeaveStatuses.Rejected;
        request.CurrentStep = LeaveSteps.Completed;
        request.UpdatedAt = DateTime.UtcNow;

        _dbContext.ApprovalHistories.Add(new ApprovalHistory
        {
            LeaveRequestId = request.Id,
            ApproverRole = "HR",
            StepName = LeaveSteps.HrReview,
            Action = isApproved ? "Approved" : "Rejected",
            Comment = dto.Comment,
            ActionAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (true, "HR xử lý thành công.");
    }
}