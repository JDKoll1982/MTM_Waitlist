using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Resolves the single effective mock decision for an external data source. This is the deterministic
/// precedence rule that every caller (helper-server routing, Developer-UI toggle state, health-driven
/// auto-fallback) should share so behaviour is consistent:
/// <list type="number">
/// <item>If the source is unreachable (or not configured), mock is <b>forced on</b> and the toggle disabled.</item>
/// <item>Else if a local manual override exists, it wins.</item>
/// <item>Else if a central shared setting is present, it governs.</item>
/// <item>Else fall back to live data (mock off).</item>
/// </list>
/// The service is intentionally pure (no dependencies) so the rule is trivially unit-testable; callers supply
/// the resolved inputs.
/// </summary>
public interface IMockRoutingService
{
    /// <summary>Computes the effective mock decision from the resolved inputs for one source.</summary>
    MockRoutingDecision Resolve(
        ConnectionSource source,
        bool? localManualOverride,
        MockSettingState centralConfig,
        ConnectionHealthState health);
}
