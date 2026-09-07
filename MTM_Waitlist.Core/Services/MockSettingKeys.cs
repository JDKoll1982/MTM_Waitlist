using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// Single source of truth for the local/central mock setting key of each external data source.
/// Both <c>MockConfigurationService</c> (central all_users store) and <c>MockRoutingCoordinator</c>
/// (per-machine manual override) use the same keys so the two stores stay aligned.
/// </summary>
public static class MockSettingKeys
{
    public static string For(ConnectionSource source) => source switch
    {
        ConnectionSource.InforVisual => "Feature.InforVisualMockData",
        ConnectionSource.Receiving => "Feature.RecvMockData",
        _ => string.Empty,
    };
}
