using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Pure filtering for the Waitlist detail location grid and for the Part stock window, which ask two different
/// questions of the same rows.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Apply"/> answers the <b>grid's</b> question: which locations matter for picking the part up, so only
/// rows with an on-hand quantity of at least 1 remain and rows whose location is in the ignored-locations set
/// (file 05) are omitted. Rows are returned ordered by location, then part number, for stable display.
/// </para>
/// <para>
/// <see cref="HoldingLocations"/> answers the <b>Part stock window's</b> question instead: where is this material
/// held at all. Nothing is hidden from it, because the window draws those rows beside the part-wide total the card
/// already reports and the two have to agree — a part whose whole stock sits in an ignored location must still show
/// that location rather than an empty list beside a non-zero total.
/// </para>
/// </remarks>
public static class InventoryLocationFiltering
{
    public static IReadOnlyList<InventoryLocationRow> Apply(
        IEnumerable<InventoryLocationRow> rows,
        IReadOnlyCollection<string> ignoredLocations)
    {
        var ignored = new HashSet<string>(
            ignoredLocations?
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim()) ?? Enumerable.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);

        return Filter(rows, ignored);
    }

    /// <summary>
    /// Every location that holds stock of the part, with no location hidden: the Part stock window's own answer.
    /// </summary>
    /// <param name="rows">The rows the read returned.</param>
    /// <returns>The rows holding at least one unit, in location order.</returns>
    public static IReadOnlyList<InventoryLocationRow> HoldingLocations(IEnumerable<InventoryLocationRow> rows) =>
        Filter(rows, s_noHiddenLocations);

    private static readonly HashSet<string> s_noHiddenLocations = new(StringComparer.OrdinalIgnoreCase);

    private static IReadOnlyList<InventoryLocationRow> Filter(
        IEnumerable<InventoryLocationRow> rows,
        HashSet<string> ignored)
    {
        if (rows is null)
        {
            return Array.Empty<InventoryLocationRow>();
        }

        return rows
            .Where(row => row is not null)
            .Where(row => row.OnHandQuantity >= 1m)
            .Where(row => string.IsNullOrWhiteSpace(row.Location) || !ignored.Contains(row.Location.Trim()))
            .OrderBy(row => row.Location.Trim(), StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.PartNumber, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
