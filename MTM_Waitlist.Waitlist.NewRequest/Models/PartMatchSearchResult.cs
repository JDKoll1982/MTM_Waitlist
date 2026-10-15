namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// The outcome of one fuzzy incorrect-part search: the term the search was asked about — already through the
/// input rule — and what the live plant-system read answered (<c>data-model.md</c> §11).
/// </summary>
/// <remarks>
/// <para>
/// An exact match and a list of containing parts are two different answers, not one answer with a flag: the exact
/// match resolves the part at once, while a list is offered for the operator to choose from (FR-083).
/// </para>
/// <para>
/// <see cref="Unavailable"/> and <see cref="Failed"/> are deliberately distinct. The first is the plant system
/// being unreachable, which the spec calls the designed behaviour: the operator is told, and the value they typed
/// is used (FR-085). The second is a fault — the read's own script missing, or a connection refused — which is
/// reported as itself rather than disguised as an outage, and still lets the operator proceed.
/// </para>
/// </remarks>
public sealed record PartMatchSearchResult
{
    /// <summary>How the search ended.</summary>
    public required PartMatchSearchStatus Status { get; init; }

    /// <summary>The term the read was asked about, through the search's own input rule (FR-082).</summary>
    public string Term { get; init; } = string.Empty;

    /// <summary>The part the term matched exactly, or null when nothing matched exactly (FR-083).</summary>
    public string? ExactMatch { get; init; }

    /// <summary>
    /// Every part whose number contains the term, in the read's own order, or empty when nothing matched.
    /// </summary>
    public IReadOnlyList<string> Matches { get; init; } = [];

    /// <summary>Diagnostic detail for a search that could not answer, or null.</summary>
    public string? Reason { get; init; }

    /// <summary>Nothing was searched.</summary>
    /// <param name="term">The normalized term, which is empty.</param>
    public static PartMatchSearchResult NothingToSearch(string term) =>
        new() { Status = PartMatchSearchStatus.NothingToSearch, Term = term };

    /// <summary>The term matched a part number exactly.</summary>
    /// <param name="term">The normalized term.</param>
    /// <param name="partNumber">The part the term matched.</param>
    public static PartMatchSearchResult Resolved(string term, string partNumber) =>
        new() { Status = PartMatchSearchStatus.Resolved, Term = term, ExactMatch = partNumber };

    /// <summary>Nothing matched exactly, and the containing parts are offered.</summary>
    /// <param name="term">The normalized term.</param>
    /// <param name="matches">The parts whose numbers contain the term.</param>
    public static PartMatchSearchResult Contains(string term, IReadOnlyList<string> matches) =>
        new() { Status = PartMatchSearchStatus.Contains, Term = term, Matches = matches };

    /// <summary>The plant system could not be reached (FR-085).</summary>
    /// <param name="term">The normalized term.</param>
    /// <param name="reason">Diagnostic detail, which is logged and never drawn on the step.</param>
    public static PartMatchSearchResult Unavailable(string term, string? reason) =>
        new() { Status = PartMatchSearchStatus.Unavailable, Term = term, Reason = reason };

    /// <summary>The read could not be completed (FR-084).</summary>
    /// <param name="term">The normalized term.</param>
    /// <param name="reason">Diagnostic detail, which is logged and never drawn on the step.</param>
    public static PartMatchSearchResult Failed(string term, string? reason) =>
        new() { Status = PartMatchSearchStatus.Failed, Term = term, Reason = reason };
}
