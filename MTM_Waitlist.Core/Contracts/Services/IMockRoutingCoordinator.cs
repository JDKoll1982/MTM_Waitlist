using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Composes the mock decision for an external data source from its three inputs — live connection health,
/// the central shared setting, and the per-machine local override — and runs them through the shared
/// <c>IMockRoutingService</c> precedence rule. Callers ask this one question instead of wiring the pieces
/// themselves, so helper-server routing and the Developer-UI toggle stay consistent.
/// </summary>
public interface IMockRoutingCoordinator
{
    /// <summary>
    /// Returns the effective mock decision for one source.
    /// When <paramref name="refreshHealth"/> is true a fresh health probe runs; otherwise the last known
    /// snapshot (or <c>Unknown</c>) is used so hot call paths don't block on the network.
    /// </summary>
    Task<MockRoutingDecision> GetEffectiveDecisionAsync(
        ConnectionSource source,
        bool refreshHealth = false,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the effective decision for every external source using their last known health.</summary>
    Task<IReadOnlyDictionary<ConnectionSource, MockRoutingDecision>> GetEffectiveDecisionsAsync(
        CancellationToken cancellationToken = default);
}
