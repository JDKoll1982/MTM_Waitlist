using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class MockModePollingHostTests
{
    [TestMethod]
    public async Task Start_RaiseTick_RunsRefreshAndRaisesModeChanged()
    {
        var refresh = new StubRefreshService(new[] { Change(MockModeChangeKind.ForcedOn) });
        var monitor = new MockRoutingMonitorService(refresh);
        var scheduler = new FakeScheduler();
        var host = new MockModePollingHost(monitor, scheduler);

        var raised = new List<MockModeChange>();
        host.ModeChanged += (_, change) => raised.Add(change);

        host.Start();
        Assert.IsTrue(host.IsRunning);

        scheduler.RaiseTick();
        await Task.Delay(30); // allow the async-void tick handler to complete

        Assert.AreEqual(1, raised.Count);
        Assert.AreEqual(MockModeChangeKind.ForcedOn, raised[0].Kind);
        Assert.AreEqual(1, host.LastChanges.Count);
        Assert.IsTrue(refresh.RunCount >= 1);

        host.Stop();
        Assert.IsFalse(host.IsRunning);
    }

    [TestMethod]
    public void StartStop_ForwardToScheduler()
    {
        var scheduler = new FakeScheduler();
        var host = new MockModePollingHost(new MockRoutingMonitorService(new StubRefreshService(Array.Empty<MockModeChange>())), scheduler);

        host.Start();
        Assert.IsTrue(scheduler.IsRunning);
        host.Stop();
        Assert.IsFalse(scheduler.IsRunning);
        host.Stop(); // idempotent
    }

    private static MockModeChange Change(MockModeChangeKind kind) => new()
    {
        Source = ConnectionSource.Receiving,
        Kind = kind,
        UseMockDataAfter = true,
        Message = "x",
    };

    private sealed class FakeScheduler : IPollScheduler
    {
        public event EventHandler? Tick;

        public bool IsRunning { get; private set; }

        public void Start() => IsRunning = true;

        public void Stop() => IsRunning = false;

        public void RaiseTick() => Tick?.Invoke(this, EventArgs.Empty);
    }

    private sealed class StubRefreshService : IMockRoutingRefreshService
    {
        private readonly IReadOnlyList<MockModeChange> _changes;

        public StubRefreshService(IReadOnlyList<MockModeChange> changes) => _changes = changes;

        public int RunCount { get; private set; }

        public IReadOnlyDictionary<ConnectionSource, MockRoutingDecision> LastDecisions { get; } = new Dictionary<ConnectionSource, MockRoutingDecision>();

        public Task<IReadOnlyList<MockModeChange>> RefreshAsync(CancellationToken cancellationToken = default)
        {
            RunCount++;
            return Task.FromResult(_changes);
        }

        public void Reset() { }
    }
}
