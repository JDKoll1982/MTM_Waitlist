using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// Pure helper that turns mock-mode decisions/changes into user-facing status and toast text, so the Developer
/// status card (F13) and the Phase 1.3 toasts all present a source name + clear message consistently.
/// Deterministic and unit-testable.
/// </summary>
public static class MockModeSummaryProvider
{
    /// <summary>Human display name for a source (used in status lines and toasts).</summary>
    public static string SourceDisplayName(ConnectionSource source) => source switch
    {
        ConnectionSource.InforVisual => "Infor Visual",
        ConnectionSource.Receiving => "Receiving",
        _ => source.ToString(),
    };

    /// <summary>A one-line status description for the current decision of a source (status card).</summary>
    public static string Describe(MockRoutingDecision decision)
    {
        var source = SourceDisplayName(decision.Source);
        if (decision.IsAutoForced)
        {
            return $"{source} is unreachable — mock data is forced on.";
        }

        return decision.UseMockData
            ? $"{source} is using mock data."
            : $"{source} is using live data.";
    }

    /// <summary>
    /// The user-facing toast/notification message for a <see cref="MockModeChange"/> (names the affected source).
    /// Falls back to the change's own Message when present.
    /// </summary>
    public static string ToastMessage(MockModeChange change)
    {
        var source = SourceDisplayName(change.Source);
        return change.Kind switch
        {
            MockModeChangeKind.ForcedOn => $"{source} unreachable — switched to mock data.",
            MockModeChangeKind.Recovered => change.UseMockDataAfter
                ? $"{source} is back online — mock data stays on by configuration."
                : $"{source} is back online — live data restored.",
            MockModeChangeKind.TurnedOn => $"{source} is now using mock data.",
            MockModeChangeKind.TurnedOff => $"{source} is now using live data.",
            MockModeChangeKind.StillForced => $"{source} is still unreachable — mock data remains on.",
            _ => $"{source}: {change.Message}".TrimEnd(':', ' '),
        };
    }
}
