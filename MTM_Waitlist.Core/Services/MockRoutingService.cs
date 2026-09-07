using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IMockRoutingService"/>
public sealed class MockRoutingService : IMockRoutingService
{
    public MockRoutingDecision Resolve(
        ConnectionSource source,
        bool? localManualOverride,
        MockSettingState centralConfig,
        ConnectionHealthState health)
    {
        // 1) Source unreachable (or not configured) => force mock on; disable the toggle so the user cannot
        //    turn it off against a source the app cannot reach.
        if (health?.Status == ConnectionHealthStatus.Unreachable)
        {
            return new MockRoutingDecision
            {
                Source = source,
                UseMockData = true,
                IsAutoForced = true,
                Reason = MockRoutingReason.SourceUnreachable,
                ReasonText = "Source unreachable — mock data is temporarily forced on.",
            };
        }

        // 2) Reachable (or still unknown) => honour a local manual override if one is set.
        if (localManualOverride is not null)
        {
            var manual = localManualOverride.Value;
            return new MockRoutingDecision
            {
                Source = source,
                UseMockData = manual,
                IsAutoForced = false,
                Reason = MockRoutingReason.ManualOverride,
                ReasonText = manual
                    ? "Mock data on (local override)."
                    : "Live data (local override).",
            };
        }

        // 3) No local override => use the centrally-stored shared setting when present.
        if (centralConfig?.IsPresent == true)
        {
            return new MockRoutingDecision
            {
                Source = source,
                UseMockData = centralConfig.IsMockEnabled,
                IsAutoForced = false,
                Reason = MockRoutingReason.CentralConfig,
                ReasonText = centralConfig.IsMockEnabled
                    ? "Mock data on (shared setting)."
                    : "Live data (shared setting).",
            };
        }

        // 4) Reachable with nothing configured anywhere => live data is the safe default.
        return new MockRoutingDecision
        {
            Source = source,
            UseMockData = false,
            IsAutoForced = false,
            Reason = MockRoutingReason.DefaultOff,
            ReasonText = "Live data (default).",
        };
    }
}
