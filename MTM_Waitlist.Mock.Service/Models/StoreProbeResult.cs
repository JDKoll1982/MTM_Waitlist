namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// The result of one store connectivity probe.
/// </summary>
/// <remarks>
/// <see cref="IsConfigured"/> is deliberately separate from <see cref="IsReachable"/>: a store with no
/// connection string has not been <i>proved</i> unreachable, and reporting it as such would disable backup
/// and restore for a host whose credentials arrive later (through the environment, or through a settings save).
/// </remarks>
/// <param name="IsConfigured">Whether the service has a connection string for the store at all.</param>
/// <param name="IsReachable">Whether the database accepted a connection.</param>
/// <param name="Reason">Why it could not be reached, or <see langword="null"/> when it could.</param>
public sealed record StoreProbeResult(bool IsConfigured, bool IsReachable, string? Reason)
{
    /// <summary>No connection string is configured for the store: nothing is known about it.</summary>
    public static StoreProbeResult NotConfigured { get; } = new(IsConfigured: false, IsReachable: false, Reason: null);

    /// <summary>The database accepted a connection.</summary>
    public static StoreProbeResult Reachable { get; } = new(IsConfigured: true, IsReachable: true, Reason: null);

    /// <summary>The database did not accept a connection.</summary>
    /// <param name="reason">Why, in operator-readable terms. Never includes credential material.</param>
    public static StoreProbeResult Unreachable(string reason) =>
        new(IsConfigured: true, IsReachable: false, reason);
}
