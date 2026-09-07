using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Pure filtering for the Waitlist detail location grid: only rows with on-hand quantity
/// &gt;= 1 remain, and rows whose Location is in the ignored-locations set (file 05) are
/// omitted. Rows are returned ordered by Location, then PartNumber for stable display.
/// </summary>
public static class InventoryLocationFiltering
{
    public static IReadOnlyList<InventoryLocationRow> Apply(
        IEnumerable<InventoryLocationRow> rows,
        IReadOnlyCollection<string> ignoredLocations)
    {
        if (rows is null)
        {
            return Array.Empty<InventoryLocationRow>();
        }

        var ignored = new HashSet<string>(
            ignoredLocations?
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim()) ?? Enumerable.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);

        return rows
            .Where(row => row is not null)
            .Where(row => row.OnHandQuantity >= 1m)
            .Where(row => string.IsNullOrWhiteSpace(row.Location) || !ignored.Contains(row.Location.Trim()))
            .OrderBy(row => row.Location.Trim(), StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.PartNumber, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
