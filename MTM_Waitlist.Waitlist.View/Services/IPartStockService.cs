namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>The system a part's stock answer came from.</summary>
public enum PartStockSource
{
    /// <summary>The MTM WIP Application's own floor inventory.</summary>
    WipApp,

    /// <summary>Infor Visual (live, or its mirror while Visual is unreachable).</summary>
    Visual,
}

/// <summary>One location a part is held in, and how much is held there.</summary>
/// <param name="Location">The location name or code.</param>
/// <param name="Quantity">The quantity held there.</param>
public sealed record PartStockLocation(string Location, decimal Quantity);

/// <summary>Where a part is held, how much is on hand in total, and which system said so.</summary>
/// <param name="PartNumber">The part the answer is about.</param>
/// <param name="Source">The system the answer came from.</param>
/// <param name="IsCachedData">True when a Visual answer was served from the mirror because Visual was unreachable.</param>
/// <param name="CachedAsOfUtc">When the mirror was last refreshed, for a cached answer that knows it.</param>
/// <param name="Locations">Every location that holds stock, in location order.</param>
/// <param name="TotalQuantity">The total quantity on hand across the whole part.</param>
/// <param name="WasRead">False when the lookup failed, so a zero total means "unknown" rather than "none on hand".</param>
public sealed record PartStockSnapshot(
    string PartNumber,
    PartStockSource Source,
    bool IsCachedData,
    DateTimeOffset? CachedAsOfUtc,
    IReadOnlyList<PartStockLocation> Locations,
    decimal TotalQuantity,
    bool WasRead = true);

/// <summary>
/// Answers where a part is held and how much is on hand, from the system that owns the part: the WIP
/// Application when its floor holds stock of the part, Infor Visual otherwise.
/// </summary>
public interface IPartStockService
{
    /// <summary>Reads the stock of one part. Never throws: a source that cannot be read answers with no stock.</summary>
    /// <param name="partNumber">The part to look up.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The snapshot; empty locations and a zero total when nothing is held or nothing could be read.</returns>
    Task<PartStockSnapshot> GetStockAsync(string partNumber, CancellationToken cancellationToken = default);
}

/// <summary>The seam over the WIP floor's per-location stock read, so the source rule can be tested.</summary>
public interface IWipPartStockReader
{
    /// <summary>Every location the WIP floor holds stock of the part in; empty when none or unreadable.</summary>
    Task<IReadOnlyList<MTM_Waitlist.Module_Core.Services.WipLocationQuantity>> GetLocationsAsync(
        string partNumber,
        CancellationToken cancellationToken = default);
}
