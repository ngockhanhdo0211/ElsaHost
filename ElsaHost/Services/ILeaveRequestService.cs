using ElsaHost.Entities;
using ElsaHost.Models;

namespace ElsaHost.Services;

public interface ILeaveRequestService
{
    Task<LeaveRequest> CreateAsync(CreateLeaveRequestDto dto, CancellationToken cancellationToken = default);
    Task<LeaveRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<LeaveRequest>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<ApprovalHistory>> GetHistoryAsync(int leaveRequestId, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> ManagerDecisionAsync(int id, DecisionDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> HrDecisionAsync(int id, DecisionDto dto, CancellationToken cancellationToken = default);
}