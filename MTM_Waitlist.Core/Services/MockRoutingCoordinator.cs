using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IMockRoutingCoordinator"/>
public sealed class MockRoutingCoordinator : IMockRoutingCoordinator
{
    private readonly IConnectionHealthService _connectionHealthService;
    private readonly IMockConfigurationService _mockConfigurationService;
    private readonly IMockRoutingService _mockRoutingService;
    private readonly ILocalSettingsService _localSettingsService;

    public MockRoutingCoordinator(
        IConnectionHealthService connectionHealthService,
        IMockConfigurationService mockConfigurationService,
        IMockRoutingService mockRoutingService,
        ILocalSettingsService localSettingsService)
    {
        _connectionHealthService = connectionHealthService;
        _mockConfigurationService = mockConfigurationService;
        _mockRoutingService = mockRoutingService;
        _localSettingsService = localSettingsService;
    }

    public async Task<MockRoutingDecision> GetEffectiveDecisionAsync(
        ConnectionSource source,
        bool refreshHealth = false,
        CancellationToken cancellationToken = default)
    {
        var health = refreshHealth
            ? await _connectionHealthService.CheckAsync(source, cancellationToken).ConfigureAwait(false)
            : _connectionHealthService.GetLastKnown(source);

        var central = await _mockConfigurationService
            .GetMockSettingAsync(source, cancellationToken)
            .ConfigureAwait(false);

        var localManualOverride = await _localSettingsService
            .ReadSettingAsync<bool?>(MockSettingKeys.For(source))
            .ConfigureAwait(false);

        return _mockRoutingService.Resolve(source, localManualOverride, central, health);
    }

    public async Task<IReadOnlyDictionary<ConnectionSource, MockRoutingDecision>> GetEffectiveDecisionsAsync(
        CancellationToken cancellationToken = default)
    {
        var sources = new[] { ConnectionSource.InforVisual, ConnectionSource.Receiving };
        var decisions = new Dictionary<ConnectionSource, MockRoutingDecision>();
        foreach (var source in sources)
        {
            decisions[source] = await GetEffectiveDecisionAsync(source, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        return decisions;
    }
}
