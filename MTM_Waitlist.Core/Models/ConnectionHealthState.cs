namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// The external data source whose reachability drives whether the app uses live data or falls back to mock.
/// <c>mtm_waitlist</c> is intentionally excluded: it is required for the app to start at all.
/// </summary>
public enum ConnectionSource
{
    /// <summary>The Infor Visual SQL Server database (read-only lookups for coil/inventory/setup).</summary>
    InforVisual,

    /// <summary>The receiving application MySQL database (avg coil weight, flatstock/receiving reads).</summary>
    Receiving,
}

/// <summary>Reachability of an external data source.</summary>
public enum ConnectionHealthStatus
{
    /// <summary>Health has not been checked yet this session.</summary>
    Unknown,

    /// <summary>A connection could be opened; live data is available.</summary>
    Connected,

    /// <summary>A connection could not be opened; the app should fall back to mock for this source.</summary>
    Unreachable,
}

/// <summary>
/// A point-in-time reachability snapshot for one external source.
/// </summary>
public sealed record ConnectionHealthState
{
    public ConnectionSource Source { get; init; }

    public ConnectionHealthStatus Status { get; init; } = ConnectionHealthStatus.Unknown;

    public DateTimeOffset CheckedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Optional user-safe detail (e.g. "server unreachable" vs "bad credentials"); never raw secrets.</summary>
    public string Reason { get; init; } = string.Empty;

    public bool IsUnreachable => Status == ConnectionHealthStatus.Unreachable;
}
