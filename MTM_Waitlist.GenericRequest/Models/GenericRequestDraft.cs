using MTM_Waitlist.GenericRequest.Contracts;

namespace MTM_Waitlist.GenericRequest.Models;

/// <summary>
/// The answers given so far. One step writes its own answer and no step clears another's, so going back keeps
/// everything already chosen.
/// </summary>
public sealed class GenericRequestDraft
{
    /// <summary>The work center the request belongs to.</summary>
    public string WorkCenter { get; set; } = string.Empty;

    /// <summary>The building the chosen work center stands in, which is what the request is filed under.</summary>
    public string Building { get; set; } = string.Empty;

    /// <summary>The category the requester chose.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>The item's display name, which is what the summary and the card say.</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>The item's stable catalog code, which is what the store is written from.</summary>
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>
    /// The answer the item's own configuration asked for, or empty when the item asks nothing. This is the value
    /// the request is about, and the one a generic card states (FR-007, FR-023).
    /// </summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// The allotment configured for the chosen item, or null when it configures none. Null is not zero: the review
    /// states no estimate rather than invent a default (FR-013, FR-014).
    /// </summary>
    public int? EstimatedMinutes { get; set; }

    /// <summary>
    /// Every warning the requester accepted, in the order they were shown. Empty when none was shown, which is not
    /// the same as a warning that was shown and dismissed (FR-019).
    /// </summary>
    public List<string> AcceptedWarnings { get; } = new();

    public string PartNumber { get; set; } = string.Empty;

    /// <summary>
    /// The stage the chosen sequence's operation state gave, derived by the quantity read and never chosen by the
    /// requester (FR-009, FR-012).
    /// </summary>
    public GenericRequestPartStage PartStage { get; set; } = GenericRequestPartStage.Unknown;

    public string Sequence { get; set; } = string.Empty;

    /// <summary>
    /// Which systems supplied the chosen sequence, joined in the order the list showed them. This is what the
    /// request records (FR-016).
    /// </summary>
    public string SequenceSource { get; set; } = string.Empty;

    /// <summary>
    /// The quantity reading for the chosen sequence: a figure with the system that answered, or not inventoried.
    /// Null until a sequence is chosen, and cleared whenever the part changes (FR-008).
    /// </summary>
    public GenericRequestQuantityReading? QuantityReading { get; set; }

    public string DunnageType { get; set; } = string.Empty;

    public string DunnagePart { get; set; } = string.Empty;

    public string ScrapType { get; set; } = string.Empty;

    public string DieNumber { get; set; } = string.Empty;

    public double Quantity { get; set; } = 1;

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether the chosen item asks for a proved part. False here means the item states a configured answer or
    /// nothing at all, so the part step is not visited (FR-007, FR-012).
    /// </summary>
    public bool RequiresPart { get; set; }

    /// <summary>Whether the chosen item asks for the dunnage page.</summary>
    public bool RequiresDunnage { get; set; }

    /// <summary>Whether the chosen item asks for the scrap page.</summary>
    public bool RequiresScrap { get; set; }

    /// <summary>Whether the chosen item asks for the die page.</summary>
    public bool RequiresDie { get; set; }

    /// <summary>
    /// The request number the result step shows once the request has been raised: the store's own identifier for the
    /// request, which is also what the board targets it by.
    /// </summary>
    public string RaisedRequestNumber { get; set; } = string.Empty;

    /// <summary>
    /// What the result step shows. The raise attempt decides it, so an unraised request reads empty, a request the
    /// store accepted reads populated, and a failed one reads unavailable with the attempt time and a retry.
    /// </summary>
    public GenericRequestPresentation ResultPresentation { get; set; } = GenericRequestPresentation.Empty;

    /// <summary>Why the raise attempt did not land, in plain language, or empty when it did or has not been tried.</summary>
    public string ResultMessage { get; set; } = string.Empty;

    /// <summary>When the raise attempt was made, in UTC, so the result step can show it beside a retry.</summary>
    public DateTimeOffset? ResultAttemptedAtUtc { get; set; }
}
