using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Sample per-part inventory rows for the Waitlist detail location grid (mock mode:
/// <c>Feature.InforVisualMockData</c> ON).
///
/// Each row is ONE pooled record per unique part-in-location, mirroring Infor Visual: a
/// location appears once and pools all transactions for that part number into a single
/// total on-hand quantity, expressed as total WEIGHT in pounds (e.g. three coils of
/// MMC0001000 @ 5,000 lb in V-A0-01 pool to one row of 15,000 lb). Locations are Infor
/// location codes (V-A0-01 style), not storage labels.
///
/// Rows deliberately include ignored-location codes (WC/NCM defaults from file 05) and a
/// zero-quantity row so the grid's "on-hand &gt;= 1" and "omit ignored locations" rules are
/// exercised with mock ON.
/// </summary>
public static class SampleInventoryLocationCatalog
{
    public static IReadOnlyList<InventoryLocationRow> GetRows(string? partNumber = null)
    {
        var normalizedPart = string.IsNullOrWhiteSpace(partNumber) ? "MMC0001000" : partNumber.Trim();

        return new[]
        {
            // Pooled per-location on-hand weight (lb) for the part. These are non-ignored,
            // non-zero rows -> shown in the grid.
            new InventoryLocationRow { PartNumber = normalizedPart, Location = "V-A0-01", OnHandQuantity = 15000m },
            new InventoryLocationRow { PartNumber = normalizedPart, Location = "V-A0-04", OnHandQuantity = 25000m },
            new InventoryLocationRow { PartNumber = normalizedPart, Location = "V-B2-10", OnHandQuantity = 6000m },
            // Ignored locations (file 05 defaults) -> omitted from the grid and totals.
            new InventoryLocationRow { PartNumber = normalizedPart, Location = "WC", OnHandQuantity = 2000m },
            new InventoryLocationRow { PartNumber = normalizedPart, Location = "NCM", OnHandQuantity = 1000m },
            new InventoryLocationRow { PartNumber = normalizedPart, Location = "V-WC", OnHandQuantity = 500m },
            new InventoryLocationRow { PartNumber = normalizedPart, Location = "NCM-VITS", OnHandQuantity = 1500m },
            new InventoryLocationRow { PartNumber = normalizedPart, Location = "SHIP", OnHandQuantity = 4000m },
            // Non-ignored but zero on-hand weight -> dropped by the "on-hand &gt;= 1" rule.
            new InventoryLocationRow { PartNumber = normalizedPart, Location = "V-A0-00", OnHandQuantity = 0m },
        };
    }
}
