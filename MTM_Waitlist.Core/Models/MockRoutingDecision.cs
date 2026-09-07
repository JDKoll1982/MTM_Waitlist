namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// Why the app resolved a particular effective mock mode for an external data source. Kept coarse so
/// callers (UI toggle state, health-driven toasts, helper-server routing) can branch without parsing text.
/// </summary>
public enum MockRoutingReason
{
    /// <summary>The source is unreachable / not configured, so mock is forced on and the toggle is disabled.</summary>
    SourceUnreachable,

    /// <summary>A local per-machine manual override is set and the source is reachable.</summary>
    ManualOverride,

    /// <summary>No local override; the centrally-stored (all_users) setting governs and the source is reachable.</summary>
    CentralConfig,

    /// <summary>Reachable but nothing configured anywhere; safe default is live data (mock off).</summary>
    DefaultOff,
}

/// <summary>
/// The resolved effective mock decision for one external data source, produced by <c>MockRoutingService</c>.
/// Combines the local manual override, the central shared configuration, and the live connection-health
/// snapshot so all callers agree on one answer.
/// </summary>
public sealed record MockRoutingDecision
{
    public ConnectionSource Source { get; init; }

    /// <summary>True when the effective behavior for this source is to use mock data.</summary>
    public bool UseMockData { get; init; }

    /// <summary>
    /// True when the source is unreachable and mock is therefore forced on. The user-facing toggle should
    /// be disabled while <c>IsAutoForced</c> is true so it cannot be turned off against an unreachable source.
    /// </summary>
    public bool IsAutoForced { get; init; }

    public MockRoutingReason Reason { get; init; }

    /// <summary>User-safe one-line description of the decision (for tooltips / status text).</summary>
    public string ReasonText { get; init; } = string.Empty;
}
