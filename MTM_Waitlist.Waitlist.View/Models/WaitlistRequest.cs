namespace MTM_Waitlist.Module_Waitlist.Models;

public sealed class WaitlistRequest
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Building { get; init; } = string.Empty;
    public string WorkCenter { get; init; } = string.Empty;

    /// <summary>The stored umbrella Category (FR-004).</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>The stored Item code (FR-004).</summary>
    public string Item { get; init; } = string.Empty;

    /// <summary>
    /// Transitional: the display pair the list and the request page still read until US2 re-points them at the
    /// Item. It is projected from <see cref="Item"/> on read, never from a stored column — the retired
    /// <c>request_type</c> and <c>subtype</c> columns are gone (FR-004, FR-023).
    /// </summary>
    public string RequestType { get; init; } = string.Empty;
    public string? Subtype { get; init; }
    public string? InputValue { get; init; }
    public string ActiveSetupJobId { get; init; } = string.Empty;
    public string WorkCenterName { get; init; } = string.Empty;
    public string RequesterEmployeeNumber { get; init; } = string.Empty;
    public string RequesterEmployeeName { get; init; } = string.Empty;
    public string Status { get; init; } = "Pending";
    public DateTimeOffset RequestedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? TargetTimeUtc { get; init; }
    public bool IsOverdue { get; init; }
    public string? AssignedMaterialHandler { get; init; }
    public string? CancellationReason { get; init; }
    public DateTimeOffset? CanceledUtc { get; init; }
    public string? CanceledByEmployeeNumber { get; init; }
    public string? Note { get; init; }
    public DateTimeOffset? AcceptedUtc { get; init; }
    public DateTimeOffset? CompletedUtc { get; init; }
    public DateTimeOffset? ReleasedUtc { get; init; }

    /// <summary>
    /// When the stored row last changed, from the queue table. The list uses it as the cheap "something
    /// happened on this request" signal that drives the new-message indicator on the card, so the indicator
    /// does not need a history read per row.
    /// </summary>
    public DateTimeOffset? UpdatedUtc { get; init; }

    /// <summary>
    /// When the newest message written by a person was added — a note, not a lifecycle change. This is what the
    /// card's new-message marker compares against: <see cref="UpdatedUtc"/> moves for every change, including the
    /// system events (created, accepted, completed, canceled) that nobody sent and nobody needs telling about.
    /// Null when no person has written anything on the request yet.
    /// </summary>
    public DateTimeOffset? LastMessageUtc { get; init; }
}