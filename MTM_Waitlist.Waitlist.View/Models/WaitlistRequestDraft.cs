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

    /// <summary>
    /// The answers the flow captured beyond <see cref="InputValue"/>, keyed by the label the Item declared.
    /// A blank value is dropped on the way to the store rather than written, so no cell is added for it
    /// (FR-006, FR-073). Null when the draft captured none.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Answers { get; init; }

    public string ActiveSetupJobId { get; init; } = string.Empty;

    /// <summary>
    /// Whether this request is raised at a generic work center, with no work order anywhere (FR-008, FR-009).
    /// A generic draft carries a blank <see cref="ActiveSetupJobId"/> and this flag is what says so on purpose,
    /// rather than the blank standing in for a value that went missing.
    /// </summary>
    public bool IsGeneric { get; init; }

    /// <summary>The requester's optional free-text description (FR-016). Distinct from <see cref="Note"/>, which
    /// the material handler writes.</summary>
    public string? Description { get; init; }

    /// <summary>Every warning the requester accepted when raising the request (FR-019), or null when none was
    /// shown.</summary>
    public IReadOnlyList<string>? AcceptedWarnings { get; init; }

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