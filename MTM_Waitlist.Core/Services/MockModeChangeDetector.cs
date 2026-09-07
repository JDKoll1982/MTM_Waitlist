using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// Pure helper that turns two consecutive <see cref="MockRoutingDecision"/> values (previous and current) into
/// a <see cref="MockModeChange"/>. Intentionally dependency-free and deterministic so the toast / recovery
/// re-enable logic is trivially unit-testable and consistent across callers.
/// </summary>
public static class MockModeChangeDetector
{
    /// <summary>
    /// Detects the change from <paramref name="previous"/> to <paramref name="current"/> for
    /// <paramref name="source"/>. A null <paramref name="previous"/> (first observation this session) yields
    /// <see cref="MockModeChangeKind.None"/> — we never toast on initial baseline.
    /// </summary>
    public static MockModeChange Detect(ConnectionSource source, MockRoutingDecision? previous, MockRoutingDecision current)
    {
        if (previous is null)
        {
            return new MockModeChange
            {
                Source = source,
                Kind = MockModeChangeKind.None,
                UseMockDataAfter = current.UseMockData,
                IsAutoForcedAfter = current.IsAutoForced,
            };
        }

        var before = previous;
        var after = current;

        // Still unreachable and forced both times → refresh, no mode flip.
        if (before.IsAutoForced && after.IsAutoForced)
        {
            return new MockModeChange
            {
                Source = source,
                Kind = MockModeChangeKind.StillForced,
                UseMockDataBefore = before.UseMockData,
                UseMockDataAfter = after.UseMockData,
                IsAutoForcedAfter = true,
                Message = "Source still unreachable — mock data remains on.",
            };
        }

        // Was forced on, now not forced → source recovered; mode follows the restored configuration.
        if (before.IsAutoForced && !after.IsAutoForced)
        {
            return new MockModeChange
            {
                Source = source,
                Kind = MockModeChangeKind.Recovered,
                UseMockDataBefore = true,
                UseMockDataAfter = after.UseMockData,
                IsAutoForcedAfter = false,
                Message = after.UseMockData
                    ? "Source is back online — mock data stays on by configuration."
                    : "Source is back online — live data restored.",
            };
        }

        // Now forced on but wasn't before → outage forced mock on.
        if (!before.IsAutoForced && after.IsAutoForced)
        {
            return new MockModeChange
            {
                Source = source,
                Kind = MockModeChangeKind.ForcedOn,
                UseMockDataBefore = before.UseMockData,
                UseMockDataAfter = true,
                IsAutoForcedAfter = true,
                Message = "Source unreachable — switched to mock data.",
            };
        }

        // Reachable both times: compare the effective mock flag (user / shared setting changed it).
        if (before.UseMockData != after.UseMockData)
        {
            var turnedOn = after.UseMockData;
            return new MockModeChange
            {
                Source = source,
                Kind = turnedOn ? MockModeChangeKind.TurnedOn : MockModeChangeKind.TurnedOff,
                UseMockDataBefore = before.UseMockData,
                UseMockDataAfter = after.UseMockData,
                IsAutoForcedAfter = false,
                Message = turnedOn ? "Mock data on." : "Live data on.",
            };
        }

        return new MockModeChange
        {
            Source = source,
            Kind = MockModeChangeKind.None,
            UseMockDataBefore = before.UseMockData,
            UseMockDataAfter = after.UseMockData,
            IsAutoForcedAfter = after.IsAutoForced,
        };
    }
}
