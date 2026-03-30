namespace ElsaHost.Models;

public class UpdateLeaveRequestStatusDto
{
    public int LeaveRequestId { get; set; }
    public string Status { get; set; } = default!;
    public string CurrentStep { get; set; } = default!;
}