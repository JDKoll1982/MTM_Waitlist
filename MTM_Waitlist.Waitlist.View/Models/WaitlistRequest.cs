namespace MTM_Waitlist.Module_Waitlist.Models;

using MTM_Waitlist.Module_Settings.Models;

public sealed class WaitlistRequest
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Building { get; init; } = string.Empty;
    public string WorkCenter { get; init; } = string.Empty;

    /// <summary>The stored umbrella Category (FR-004).</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>The stored Item code (FR-004). The request's single identity: everything the card and the
    /// request page show about *what* was asked for is resolved from this code through the Item catalog, so
    /// the request carries no second, invented pair.</summary>
    public string Item { get; init; } = string.Empty;

    /// <summary>The catalogued Item this request asks for, or null when the stored code is not in the
    /// catalog (a row the store holds but the catalog no longer describes).</summary>
    public RequestItemDefinition? ItemDefinition => RequestItemCatalog.FindById(Item);

    public string? InputValue { get; init; }

    /// <summary>
    /// The condition the request's own material handler reported from its card, or null when none is set. One
    /// of the four verbatim conditions (<c>HandlerStatusCatalog</c>); anything else is not a condition and the
    /// card falls through to the overdue marking or the lifecycle word (<c>data-model.md</c> §2, FR-012).
    /// </summary>
    public string? HandlerStatus { get; init; }

    /// <summary>
    /// The answers the request captured beyond <see cref="InputValue"/>, keyed by the label the Item declared,
    /// or null when it captured none. A blank answer is never written, so a label the requester left blank has
    /// no entry here and no cell on the card (FR-006, FR-073).
    /// </summary>
    public IReadOnlyDictionary<string, string>? Answers { get; init; }

    public string ActiveSetupJobId { get; init; } = string.Empty;
    public string WorkCenterName { get; init; } = string.Empty;
    public string RequesterEmployeeNumber { get; init; } = string.Empty;
    public string RequesterEmployeeName { get; init; } = string.Empty;
    public string Status { get; init; } = "Pending";
    public DateTimeOffset RequestedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? TargetTimeUtc { get; init; }
    public bool IsOverdue { get; init; }

    /// <summary>
    /// The material handler the request is assigned to, by employee number. This is the number every handler
    /// authorization compares, which is why the queue stores it; it is never what the screen prints.
    /// </summary>
    public string? AssignedMaterialHandler { get; init; }

    /// <summary>
    /// The assigned material handler's display name, resolved from the entry that handler wrote when they took
    /// the request, or null when no such entry names them. This is what every reader prints, because a person is
    /// named by name (FR-022).
    /// </summary>
    public string? AssignedMaterialHandlerName { get; init; }
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