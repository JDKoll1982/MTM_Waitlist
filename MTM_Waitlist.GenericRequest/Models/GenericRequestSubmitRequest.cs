namespace MTM_Waitlist.GenericRequest.Models;

/// <summary>
/// One request the generic flow wants raised, in the terms the queue's write door takes.
/// </summary>
/// <remarks>
/// <para>
/// It carries what a generic request records on itself and nothing about how the queue is reached: the submitter maps
/// it onto the queue's own draft, so this is the only shape the flow has to know
/// (024-dependency-modernization, MOD-007).
/// </para>
/// <para>
/// Every request raised through this type is a generic one, so the flag is not a field here: the flow raises at a
/// work center with no job, and there is no second kind of request it could describe.
/// </para>
/// </remarks>
public sealed record GenericRequestSubmitRequest
{
    public string Building { get; init; } = string.Empty;

    public string WorkCenter { get; init; } = string.Empty;

    /// <summary>The umbrella Category the requester chose.</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>The Item code the requester chose. The request's single identity.</summary>
    public string Item { get; init; } = string.Empty;

    /// <summary>
    /// The one value the request is about: the part a dunnage request names, the type a scrap request states, the die a
    /// die request chose, or the configured answer.
    /// </summary>
    public string? InputValue { get; init; }

    /// <summary>
    /// The answers the flow captured beyond <see cref="InputValue"/>, keyed by the label the Item declared, or null
    /// when it captured none.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Answers { get; init; }

    /// <summary>
    /// The job a filled generic request took its values from, or null when no job filled it. Provenance, not identity:
    /// the request stays generic and keeps no work order as its own.
    /// </summary>
    public string? ProvenanceWorkOrder { get; init; }

    /// <summary>The requester's optional free-text description.</summary>
    public string? Description { get; init; }

    /// <summary>Every warning the requester accepted when raising the request, or null when none was shown.</summary>
    public IReadOnlyList<string>? AcceptedWarnings { get; init; }

    public string WorkCenterName { get; init; } = string.Empty;

    public string RequesterEmployeeNumber { get; init; } = string.Empty;

    public string RequesterEmployeeName { get; init; } = string.Empty;

    public DateTimeOffset RequestedUtc { get; init; } = DateTimeOffset.UtcNow;

    public bool IsOverdue { get; init; }
}
