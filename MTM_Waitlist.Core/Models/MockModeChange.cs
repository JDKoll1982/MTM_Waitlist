namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// The kind of user-noticeable change between two consecutive mock routing decisions for one source.
/// </summary>
public enum MockModeChangeKind
{
    /// <summary>No meaningful change (or initial baseline before any prior decision).</summary>
    None,

    /// <summary>Mock switched on while the source was reachable (user/shared setting).</summary>
    TurnedOn,

    /// <summary>Mock switched off while the source was reachable (user/shared setting).</summary>
    TurnedOff,

    /// <summary>The source became unreachable, forcing mock on automatically.</summary>
    ForcedOn,

    /// <summary>The source recovered to reachable, restoring the configured (non-forced) mode.</summary>
    Recovered,

    /// <summary>The source is still unreachable but the forced decision was refreshed (no mode flip).</summary>
    StillForced,
}

/// <summary>
/// A detected change between two consecutive <see cref="MockRoutingDecision"/> values for one source, used to
/// drive the toast and any recovery re-enable logic. Pure data — no behaviour.
/// </summary>
public sealed record MockModeChange
{
    public ConnectionSource Source { get; init; }

    public MockModeChangeKind Kind { get; init; } = MockModeChangeKind.None;

    public bool UseMockDataBefore { get; init; }

    public bool UseMockDataAfter { get; init; }

    public bool IsAutoForcedAfter { get; init; }

    /// <summary>True when UI/toast should notify the user of this change.</summary>
    public bool ShouldNotify => Kind is not (MockModeChangeKind.None or MockModeChangeKind.StillForced);

    /// <summary>User-safe message describing the transition (empty when Kind is None).</summary>
    public string Message { get; init; } = string.Empty;
}
