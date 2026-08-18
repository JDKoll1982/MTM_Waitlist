namespace MTM_Waitlist.Module_Waitlist.Models;

public sealed class WaitlistRequestDraft
{
    public string Building { get; init; } = string.Empty;
    public string WorkCenter { get; init; } = string.Empty;
    public string RequestType { get; init; } = string.Empty;
    public string? Subtype { get; init; }
    public string? InputValue { get; init; }
    public string ActiveSetupJobId { get; init; } = string.Empty;
    public string WorkstationName { get; init; } = string.Empty;
    public string RequesterEmployeeNumber { get; init; } = string.Empty;
    public string RequesterEmployeeName { get; init; } = string.Empty;
    public DateTimeOffset RequestedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? TargetTimeUtc { get; init; }
    public bool IsOverdue { get; init; }
    public string? AssignedMaterialHandler { get; init; }
    public string? CancellationReason { get; init; }
    public DateTimeOffset? CanceledUtc { get; init; }
    public string? CanceledByEmployeeNumber { get; init; }
}