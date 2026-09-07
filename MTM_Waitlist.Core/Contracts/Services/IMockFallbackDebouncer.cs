using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Debounces per-source reachability so short outages/blips don't thrash the mock fallback on and off.
/// A source's <em>effective</em> status only changes after it reports a different status for
/// <see cref="Threshold"/> consecutive observations. Guards against health flapping (Phase 1.2 Security).
/// </summary>
public interface IMockFallbackDebouncer
{
    /// <summary>Number of consecutive observations required before the effective status flips.</summary>
    int Threshold { get; }

    /// <summary>
    /// Feeds one raw reachability observation for <paramref name="source"/> and returns the debounced
    /// <em>effective</em> status to use for routing. Until the threshold is met, the previous stable status
    /// is returned so a single blip cannot flip the mock fallback.
    /// </summary>
    ConnectionHealthStatus Observe(ConnectionSource source, ConnectionHealthStatus rawStatus);

    /// <summary>Clears all per-source state (e.g. on app restart or a forced re-probe).</summary>
    void Reset();
}
