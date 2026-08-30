namespace MTM_Waitlist.Module_Waitlist.Models;

public sealed class EmployeeVerificationResult
{
    public bool IsValid { get; init; }
    public bool IsActive { get; init; }
    public string EmployeeNumber { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
