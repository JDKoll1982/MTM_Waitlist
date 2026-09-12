namespace MTM_Waitlist.Mock.Service.Models;

/// <summary>
/// What this host can actually do right now, resolved from reachability rather than assumed.
/// </summary>
/// <remarks>
/// <para>
/// The service is installable on any machine, but two of its jobs need something the machine may not have:
/// <b>refreshing</b> the mirror needs Infor Visual, and <b>backing up or restoring</b> a store needs that
/// store's MySQL database. A machine that cannot reach one of them must not keep attempting the work, and
/// must say so rather than reporting a failure nobody can act on — the mirror is still perfectly serveable
/// from a host that cannot refresh it, which is the whole point of a cache.
/// </para>
/// <para>
/// <b>An unknown target is available, not disabled.</b> "Not configured" is reported elsewhere (the settings
/// surface, the cache error on the operation that needs it), and disabling refresh because a host has not been
/// pointed at Visual yet would hide a provisioning mistake behind a silent no-op.
/// </para>
/// </remarks>
/// <param name="VisualSource">Reachability of the external Infor Visual source, which gates refresh.</param>
/// <param name="Stores">Reachability of each store, which gates that store's backup and restore.</param>
/// <param name="ProbedUtc">When these answers were measured, so their age is reportable.</param>
public sealed record ServiceCapabilitySnapshot(
    VisualCapabilityState VisualSource,
    IReadOnlyDictionary<BackupStore, StoreCapabilityState> Stores,
    DateTimeOffset ProbedUtc)
{
    /// <summary>The pre-probe state: nothing known yet, so nothing is disabled.</summary>
    public static ServiceCapabilitySnapshot Unknown { get; } = new(
        VisualCapabilityState.Unspecified,
        BackupStoreExtensions.All.ToDictionary(store => store, StoreCapabilityState.Unspecified),
        DateTimeOffset.MinValue);

    /// <summary>Whether the scheduled refresh and an on-demand refresh may run.</summary>
    public bool IsRefreshAvailable => VisualSource.IsAvailable;

    /// <summary>Why refresh is disabled, or <see langword="null"/> when it is available.</summary>
    public string? RefreshUnavailableReason => VisualSource.IsAvailable ? null : VisualSource.Reason;

    /// <summary>Whether one store's backup and restore may run.</summary>
    /// <param name="store">The store to ask about.</param>
    public bool IsStoreAvailable(BackupStore store) =>
        !Stores.TryGetValue(store, out var state) || state.IsAvailable;

    /// <summary>Why one store's backup and restore are disabled, or <see langword="null"/> when they are available.</summary>
    /// <param name="store">The store to ask about.</param>
    public string? StoreUnavailableReason(BackupStore store) =>
        Stores.TryGetValue(store, out var state) && !state.IsAvailable ? state.Reason : null;
}

/// <summary>
/// Reachability of the external Infor Visual source.
/// </summary>
/// <param name="IsAvailable">Whether a refresh may run.</param>
/// <param name="Reason">Why refresh is disabled, or <see langword="null"/> when it is available.</param>
public sealed record VisualCapabilityState(bool IsAvailable, string? Reason)
{
    /// <summary>Nothing has been probed yet, or nothing is configured: refresh is not disabled.</summary>
    public static VisualCapabilityState Unspecified { get; } = new(IsAvailable: true, Reason: null);

    /// <summary>Infor Visual accepted a connection, so refresh may run.</summary>
    public static VisualCapabilityState Available { get; } = new(IsAvailable: true, Reason: null);

    /// <summary>Infor Visual could not be reached, so refresh is disabled until it can be.</summary>
    /// <param name="reason">Why it could not be reached, in operator-readable terms.</param>
    public static VisualCapabilityState Unreachable(string reason) => new(IsAvailable: false, Reason: reason);
}

/// <summary>
/// Reachability of one store's MySQL database.
/// </summary>
/// <param name="Store">The store this state describes.</param>
/// <param name="IsAvailable">Whether that store's backup and restore may run.</param>
/// <param name="Reason">Why they are disabled, or <see langword="null"/> when they are available.</param>
public sealed record StoreCapabilityState(BackupStore Store, bool IsAvailable, string? Reason)
{
    /// <summary>Nothing has been probed yet, or the store is not configured: its work is not disabled.</summary>
    /// <param name="store">The store this state describes.</param>
    public static StoreCapabilityState Unspecified(BackupStore store) => new(store, IsAvailable: true, Reason: null);

    /// <summary>The database accepted a connection, so its backup and restore may run.</summary>
    /// <param name="store">The store this state describes.</param>
    public static StoreCapabilityState Available(BackupStore store) => new(store, IsAvailable: true, Reason: null);

    /// <summary>The database could not be reached, so its backup and restore are disabled until it can be.</summary>
    /// <param name="store">The store this state describes.</param>
    /// <param name="reason">Why it could not be reached, in operator-readable terms.</param>
    public static StoreCapabilityState Unreachable(BackupStore store, string reason) =>
        new(store, IsAvailable: false, Reason: reason);
}
