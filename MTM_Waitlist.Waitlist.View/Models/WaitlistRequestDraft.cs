namespace MTM_Waitlist.Module_Waitlist.Models;

public sealed class WaitlistRequestDraft
{
    public string Building { get; init; } = string.Empty;
    public string WorkCenter { get; init; } = string.Empty;

    /// <summary>The umbrella Category the requester chose (FR-004).</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>The Item code the requester chose (FR-004). The draft's single identity: the wizard sets it
    /// from the Item step, and the store is written from it — there is no second pair to fall back on.</summary>
    public string Item { get; init; } = string.Empty;

    public string? InputValue { get; init; }
    public string ActiveSetupJobId { get; init; } = string.Empty;
    public string WorkCenterName { get; init; } = string.Empty;
    public string RequesterEmployeeNumber { get; init; } = string.Empty;
    public string RequesterEmployeeName { get; init; } = string.Empty;
    public DateTimeOffset RequestedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? TargetTimeUtc { get; init; }
    public bool IsOverdue { get; init; }
    public string? AssignedMaterialHandler { get; init; }
    public string? CancellationReason { get; init; }
    public DateTimeOffset? CanceledUtc { get; init; }
    public string? CanceledByEmployeeNumber { get; init; }
    public string? Note { get; init; }
}