using System.Globalization;

namespace MTM_Waitlist.GenericRequest.Controls.Models;

/// <summary>Which arrangement of cells the generic card draws.</summary>
public enum GenericRequestCardVariant
{
    /// <summary>The request states a configured answer: the one kind this chunk can supply in full.</summary>
    Answer,

    Part,
    Dunnage,
    Scrap,
    Die,
}

/// <summary>One entry in a request's history, shown on the detail page.</summary>
public sealed record GenericRequestCardHistoryEntry(DateTimeOffset OccurredAt, string Summary)
{
    /// <summary>When the entry happened, in local time, ready to draw.</summary>
    public string OccurredAtDisplay => OccurredAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
}

/// <summary>One cell the card draws: a label and the value under it.</summary>
/// <param name="Label">The cell's label, already localized.</param>
/// <param name="Value">The value. Never blank: the rule that builds cells omits a cell it has no value for.</param>
public sealed record GenericRequestCardCell(string Label, string Value);

/// <summary>
/// Everything the generic card and its detail page read.
/// </summary>
/// <remarks>
/// The card shows only the facts a generic request has, so a work-order value is not representable here at all. The
/// requester, the time raised and the history belong to the detail page and never to the card.
/// </remarks>
public sealed class GenericRequestCardModel
{
    public string RequestNumber { get; init; } = string.Empty;

    public GenericRequestCardVariant Variant { get; init; } = GenericRequestCardVariant.Part;

    public string WorkCenter { get; init; } = string.Empty;

    /// <summary>The request's category, in the wording a person reads (FR-024).</summary>
    public string CategoryText { get; init; } = string.Empty;

    public string ItemName { get; init; } = string.Empty;

    /// <summary>
    /// The value the requester stated, which is the request's identity alongside its work center (FR-023). For this
    /// chunk it is the answer the item's own question asked for.
    /// </summary>
    public string StatedValue { get; init; } = string.Empty;

    /// <summary>
    /// What the requester asked for in their own words, or empty when they wrote none. A request without one draws
    /// no cell for it rather than an empty one (FR-024, FR-025).
    /// </summary>
    public string Description { get; init; } = string.Empty;

    public string PartNumber { get; init; } = string.Empty;

    public string DunnageType { get; init; } = string.Empty;

    public string ScrapType { get; init; } = string.Empty;

    public string DieNumber { get; init; } = string.Empty;

    public double? Quantity { get; init; }

    public DateTimeOffset? NeededBy { get; init; }

    public string RaisedBy { get; init; } = string.Empty;

    public DateTimeOffset? RaisedAt { get; init; }

    public IReadOnlyList<GenericRequestCardHistoryEntry> History { get; init; } = Array.Empty<GenericRequestCardHistoryEntry>();
}
