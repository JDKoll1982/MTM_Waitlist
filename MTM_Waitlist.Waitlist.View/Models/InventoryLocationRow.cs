namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// A single on-hand inventory row for the Waitlist detail location grid (Part # / Location / Quantity).
///
/// Each row is the POOLED record for one unique part-in-location, as Infor Visual reports it:
/// a location appears once and pools all transactions for that part into one total on-hand
/// quantity, expressed as total WEIGHT in pounds. <see cref="Location"/> is an Infor location
/// code (e.g. V-A0-01) and <see cref="OnHandQuantity"/> is the pooled on-hand weight (lb).
/// </summary>
public sealed class InventoryLocationRow
{
    public string PartNumber { get; init; } = string.Empty;

    public string Location { get; init; } = string.Empty;

    /// <summary>Total on-hand WEIGHT (lb) pooled for the part at this location.</summary>
    public decimal OnHandQuantity { get; init; }
}
