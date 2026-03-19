namespace ElsaHost.Models;

public class CreateLeaveRequestDto
{
    public string EmployeeName { get; set; } = default!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = default!;
}