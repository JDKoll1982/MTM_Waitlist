using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Runs one client "refresh pass" over both external sources: probes fresh health, debounces it, resolves the
/// effective mock decision through the shared routing rule, and compares it to the previous decision to emit a
/// <see cref="MockModeChange"/> whenever the user should be notified (mock forced on for an outage, recovered,
/// or a manual/central toggle flip). This is the backend of the Phase 1.2 recovery handling and the Phase 2.2
/// "clients refresh central config and apply changes" flow. A UI timer drives <see cref="RefreshAsync"/>;
/// this service is the pure, testable polling/apply logic it calls.
/// </summary>
public interface IMockRoutingRefreshService
{
    /// <summary>The most recent decision per source after the last refresh (empty before the first pass).</summary>
    IReadOnlyDictionary<ConnectionSource, MockRoutingDecision> LastDecisions { get; }

    /// <summary>
    /// Performs one refresh pass over all sources and returns the notify-worthy <see cref="MockModeChange"/>s
    /// detected since the previous pass (empty on the first pass, which only establishes the baseline).
    /// </summary>
    Task<IReadOnlyList<MockModeChange>> RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>Clears the per-source last decisions so the next refresh re-establishes the baseline without events.</summary>
    void Reset();
}
