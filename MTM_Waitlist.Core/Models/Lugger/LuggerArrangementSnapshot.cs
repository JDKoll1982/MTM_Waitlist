namespace MTM_Waitlist.Module_Core.Models;

using MTM_Waitlist.Module_Core.Services;

/// <summary>
/// The whole arrangement as it stood when the arrangement read was made, and the one rule that turns it into the
/// <c>Lugger Location</c> cell's value (FR-009, FR-022, FR-024).
/// </summary>
/// <remarks>
/// <para>
/// It is read once per list load and never cached beyond it: a status change or a rename shows on the next load,
/// and no request stores anything about the lugger (FR-023).
/// </para>
/// <para>
/// The matching rule ignores letter case and surrounding spaces and nothing else, so <c>5052 aluminum</c>,
/// <c>5052 Aluminum</c> and <c> 5052 ALUMINUM </c> are one scrap type while a difference inside the spelling is
/// two (FR-009, research D5). The stored key is the lower-cased, trimmed spelling, so this type never derives a
/// key of its own.
/// </para>
/// </remarks>
public sealed class LuggerArrangementSnapshot
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<ScrapAssignment>> _byScrapTypeKey;

    private LuggerArrangementSnapshot(IReadOnlyList<ScrapAssignment> rows)
    {
        Rows = rows;

        _byScrapTypeKey = rows
            .Where(row => !string.IsNullOrWhiteSpace(row.ScrapTypeKey))
            .GroupBy(row => row.ScrapTypeKey!, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ScrapAssignment>)group
                    .OrderBy(row => row.SortPosition ?? int.MaxValue)
                    .ThenBy(row => row.LuggerName, StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);

        ScrapTypes = _byScrapTypeKey
            .Select(pair => pair.Value[0].ScrapType ?? pair.Key)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>An arrangement with nothing in it, so a failed or absent read answers no lugger rather than a guess.</summary>
    public static LuggerArrangementSnapshot Empty { get; } = new(Array.Empty<ScrapAssignment>());

    /// <summary>
    /// Every row the arrangement read returned, in its own order: one per lugger and scrap type pairing, and one
    /// for a lugger that takes no scrap type (with the assignment columns null).
    /// </summary>
    public IReadOnlyList<ScrapAssignment> Rows { get; }

    /// <summary>The scrap types the arrangement currently knows, as they are spelled, in a stable order.</summary>
    public IReadOnlyList<string> ScrapTypes { get; }

    /// <summary>Builds a snapshot over the rows of <c>sp_waitlist_lugger_arrangement_list</c>.</summary>
    public static LuggerArrangementSnapshot Create(IEnumerable<ScrapAssignment>? rows) =>
        rows is null ? Empty : new LuggerArrangementSnapshot(rows.ToArray());

    /// <summary>
    /// Resolves the job's stored scrap type to the lugger the handler walks to.
    /// <para>
    /// A scrap type with a usable dump point names that lugger. When no lugger for the type can receive material,
    /// the first lugger in the type's order is named with its status beside it, so the handler reads
    /// <c>Door 9 Lugger (Full)</c> rather than nothing (FR-024). A job with no real scrap decision and a scrap
    /// type with no assignment both resolve to nothing, and the outcome says which of the two happened.
    /// </para>
    /// </summary>
    /// <param name="scrapType">The scrap type the job recorded, exactly as stored.</param>
    public LuggerLocationResolution Resolve(string? scrapType)
    {
        if (string.IsNullOrWhiteSpace(scrapType)
            || ScrapDecisionRules.IsRequiredPlaceholder(scrapType)
            || ScrapDecisionRules.IsNoScrap(scrapType))
        {
            return new LuggerLocationResolution(null, LuggerLocationOutcome.NoScrapDecision);
        }

        var key = scrapType.Trim().ToLowerInvariant();
        if (!_byScrapTypeKey.TryGetValue(key, out var rows) || rows.Count == 0)
        {
            return new LuggerLocationResolution(null, LuggerLocationOutcome.NotConfigured);
        }

        var dumpPoint = rows.FirstOrDefault(row => row.IsDumpPoint == true && row.CanReceiveMaterial);
        if (dumpPoint is not null)
        {
            return new LuggerLocationResolution(dumpPoint.LuggerName, LuggerLocationOutcome.Resolved);
        }

        var usable = rows.FirstOrDefault(row => row.CanReceiveMaterial);
        if (usable is not null)
        {
            // The arrangement should always name a dump point when a lugger can receive material. Reaching here
            // means the flag and the statuses disagree, which only a write outside the procedures could produce;
            // naming the first usable lugger is still the true answer to "where does this material go".
            return new LuggerLocationResolution(usable.LuggerName, LuggerLocationOutcome.Resolved);
        }

        var first = rows[0];
        return new LuggerLocationResolution(
            $"{first.LuggerName} ({first.LuggerStatus})",
            LuggerLocationOutcome.Resolved);
    }
}
