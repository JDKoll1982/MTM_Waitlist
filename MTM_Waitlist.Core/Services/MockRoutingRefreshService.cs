using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IMockRoutingRefreshService"/>
public sealed class MockRoutingRefreshService : IMockRoutingRefreshService
{
    private static readonly ConnectionSource[] Sources =
    {
        ConnectionSource.InforVisual,
        ConnectionSource.Receiving,
    };

    private readonly IConnectionHealthService _connectionHealthService;
    private readonly IMockConfigurationService _mockConfigurationService;
    private readonly IMockRoutingService _mockRoutingService;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly IMockFallbackDebouncer _debouncer;

    private readonly Dictionary<ConnectionSource, MockRoutingDecision> _lastDecisions = new();

    public MockRoutingRefreshService(
        IConnectionHealthService connectionHealthService,
        IMockConfigurationService mockConfigurationService,
        IMockRoutingService mockRoutingService,
        ILocalSettingsService localSettingsService,
        IMockFallbackDebouncer debouncer)
    {
        _connectionHealthService = connectionHealthService;
        _mockConfigurationService = mockConfigurationService;
        _mockRoutingService = mockRoutingService;
        _localSettingsService = localSettingsService;
        _debouncer = debouncer;
    }

    public IReadOnlyDictionary<ConnectionSource, MockRoutingDecision> LastDecisions => _lastDecisions;

    public async Task<IReadOnlyList<MockModeChange>> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var changes = new List<MockModeChange>();
        foreach (var source in Sources)
        {
            var change = await RefreshSourceAsync(source, cancellationToken).ConfigureAwait(false);
            if (change is not null)
            {
                changes.Add(change);
            }
        }

        return changes;
    }

    public void Reset()
    {
        lock (_lastDecisions)
        {
            _lastDecisions.Clear();
        }
    }

    private async Task<MockModeChange?> RefreshSourceAsync(ConnectionSource source, CancellationToken cancellationToken)
    {
        var raw = await _connectionHealthService.CheckAsync(source, cancellationToken).ConfigureAwait(false);

        // Debounce so a single transient probe failure does not thrash the mock fallback.
        var effectiveStatus = _debouncer.Observe(source, raw.Status);
        var health = new ConnectionHealthState
        {
            Source = source,
            Status = effectiveStatus,
            CheckedUtc = raw.CheckedUtc,
            Reason = raw.Reason,
        };

        var central = await _mockConfigurationService.GetMockSettingAsync(source, cancellationToken).ConfigureAwait(false);
        var local = await _localSettingsService.ReadSettingAsync<bool?>(MockSettingKeys.For(source)).ConfigureAwait(false);

        var decision = _mockRoutingService.Resolve(source, local, central, health);

        MockRoutingDecision? previous;
        lock (_lastDecisions)
        {
            _lastDecisions.TryGetValue(source, out previous);
            _lastDecisions[source] = decision;
        }

        var change = MockModeChangeDetector.Detect(source, previous, decision);
        return change.ShouldNotify ? change : null;
    }
}
