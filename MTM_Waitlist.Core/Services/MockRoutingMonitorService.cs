using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IMockRoutingMonitorService"/>
public sealed class MockRoutingMonitorService : IMockRoutingMonitorService
{
    private readonly IMockRoutingRefreshService _refreshService;

    public MockRoutingMonitorService(IMockRoutingRefreshService refreshService)
    {
        _refreshService = refreshService;
    }

    public event EventHandler<MockModeChange>? MockModeChanged;

    public IReadOnlyList<MockModeChange> LastChanges { get; private set; } = Array.Empty<MockModeChange>();

    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        var changes = await _refreshService.RefreshAsync(cancellationToken).ConfigureAwait(false);
        LastChanges = changes;
        foreach (var change in changes)
        {
            MockModeChanged?.Invoke(this, change);
        }
    }

    public void ResetBaseline()
    {
        _refreshService.Reset();
    }
}
