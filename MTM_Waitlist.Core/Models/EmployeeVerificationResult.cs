namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// Whether the signed-in person may raise a request, judged by the account record the directory returned for their
/// own employee identifier (FR-046).
/// </summary>
/// <remarks>
/// It lives in Core beside <see cref="EmployeeIdentity"/> because two readers need it and neither may reference the
/// other: the work-order wizard that asks the question, and the generic request flow that raises a request through
/// the same door (024-dependency-modernization, MOD-007).
/// </remarks>
public sealed class EmployeeVerificationResult
{
    public bool IsValid { get; init; }
    public bool IsActive { get; init; }
    public string EmployeeNumber { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
