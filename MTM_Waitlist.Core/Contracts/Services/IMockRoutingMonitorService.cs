using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Event-driven client monitor for the central mock configuration + health auto-fallback. Wraps
/// <see cref="IMockRoutingRefreshService"/> and raises <see cref="MockModeChanged"/> for every
/// notify-worthy change (forced-on outage, recovery, or a manual/central toggle flip) so a UI host (e.g. a
/// WinUI <c>DispatcherTimer</c>) can subscribe and fire a toast. This service is the timer-decoupled polling/
/// apply core for Phase 2.2 "clients refresh central config and apply changes + fire a toast."
/// </summary>
public interface IMockRoutingMonitorService
{
    /// <summary>Raised for each notify-worthy <see cref="MockModeChange"/> detected on a refresh.</summary>
    event EventHandler<MockModeChange>? MockModeChanged;

    /// <summary>The changes raised by the most recent <see cref="RunOnceAsync"/> (empty before the first).</summary>
    IReadOnlyList<MockModeChange> LastChanges { get; }

    /// <summary>
    /// Runs one refresh pass and raises <see cref="MockModeChanged"/> for each change. The first pass only
    /// establishes the baseline (no events). Safe to call from a timer.
    /// </summary>
    Task RunOnceAsync(CancellationToken cancellationToken = default);

    /// <summary>Clears the last-decisions baseline so the next pass re-establishes without events.</summary>
    void ResetBaseline();
}
