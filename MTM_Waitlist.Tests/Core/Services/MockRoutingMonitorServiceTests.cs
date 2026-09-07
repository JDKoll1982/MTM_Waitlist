using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class MockRoutingMonitorServiceTests
{
    private static MockModeChange Change(MockModeChangeKind kind) => new()
    {
        Source = ConnectionSource.Receiving,
        Kind = kind,
        UseMockDataAfter = true,
        Message = "test",
    };

    [TestMethod]
    public async Task RunOnce_RaisesEventForEachChange()
    {
        var refresh = new StubRefreshService(new[]
        {
            Change(MockModeChangeKind.ForcedOn),
            Change(MockModeChangeKind.Recovered),
        });
        var monitor = new MockRoutingMonitorService(refresh);
        var raised = new List<MockModeChange>();
        monitor.MockModeChanged += (_, change) => raised.Add(change);

        await monitor.RunOnceAsync();

        Assert.AreEqual(2, raised.Count);
        Assert.AreEqual(MockModeChangeKind.ForcedOn, raised[0].Kind);
        Assert.AreEqual(MockModeChangeKind.Recovered, raised[1].Kind);
        Assert.AreEqual(2, monitor.LastChanges.Count);
    }

    [TestMethod]
    public async Task RunOnce_NoChanges_RaisesNothing()
    {
        var refresh = new StubRefreshService(Array.Empty<MockModeChange>());
        var monitor = new MockRoutingMonitorService(refresh);
        var raised = 0;
        monitor.MockModeChanged += (_, _) => raised++;

        await monitor.RunOnceAsync();

        Assert.AreEqual(0, raised);
        Assert.AreEqual(0, monitor.LastChanges.Count);
    }

    [TestMethod]
    public void ResetBaseline_ForwardsToRefreshService()
    {
        var refresh = new StubRefreshService(Array.Empty<MockModeChange>());
        var monitor = new MockRoutingMonitorService(refresh);

        monitor.ResetBaseline();

        Assert.IsTrue(refresh.ResetCalled);
    }

    private sealed class StubRefreshService : IMockRoutingRefreshService
    {
        private readonly IReadOnlyList<MockModeChange> _changes;

        public StubRefreshService(IReadOnlyList<MockModeChange> changes) => _changes = changes;

        public bool ResetCalled { get; private set; }

        public IReadOnlyDictionary<ConnectionSource, MockRoutingDecision> LastDecisions { get; } = new Dictionary<ConnectionSource, MockRoutingDecision>();

        public Task<IReadOnlyList<MockModeChange>> RefreshAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_changes);

        public void Reset() => ResetCalled = true;
    }
}
