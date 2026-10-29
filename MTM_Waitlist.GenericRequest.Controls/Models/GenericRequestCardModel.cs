using System.Globalization;

namespace MTM_Waitlist.GenericRequest.Controls.Models;

/// <summary>Which arrangement of cells the generic card draws.</summary>
public enum GenericRequestCardVariant
{
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

    public string ItemName { get; init; } = string.Empty;

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
