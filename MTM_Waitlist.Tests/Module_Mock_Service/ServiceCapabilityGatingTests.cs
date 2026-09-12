using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Service.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// Proof for the host capability gate: the service installs anywhere, and the work this machine cannot do is
/// disabled from reachability rather than attempted and failed.
/// </summary>
/// <remarks>
/// <para>
/// The rule under test is asymmetric on purpose: <b>evidence</b> that a target is unreachable disables its work,
/// while a target that is not configured, or a probe that could not run, disables nothing — otherwise a
/// provisioning mistake or a probe fault would look like a machine that has been proved unable to reach the
/// plant, and the work would silently never happen.
/// </para>
/// <para>
/// Nothing here touches the network: the Visual probe and the per-store probe are both stubs, so the tests are
/// the same on the cache host, a workstation, and CI.
/// </para>
/// </remarks>
[TestClass]
public sealed class ServiceCapabilityGatingTests
{
    [TestMethod]
    public async Task GetCapabilitiesAsync_WhenInforVisualIsUnreachable_DisablesRefreshWithAReasonAsync()
    {
        var probe = CreateProbe(visualConfigured: true, visualReachable: false);

        var capabilities = await probe.GetCapabilitiesAsync();

        Assert.IsFalse(capabilities.IsRefreshAvailable, "An unreachable Visual source must disable refresh on this host.");
        Assert.IsNotNull(capabilities.RefreshUnavailableReason);
        StringAssert.Contains(capabilities.RefreshUnavailableReason!, "Infor Visual");
    }

    [TestMethod]
    public async Task GetCapabilitiesAsync_WhenInforVisualIsConfiguredAndAnswers_LeavesRefreshEnabledAsync()
    {
        var probe = CreateProbe(visualConfigured: true, visualReachable: true);

        var capabilities = await probe.GetCapabilitiesAsync();

        Assert.IsTrue(capabilities.IsRefreshAvailable);
        Assert.IsNull(capabilities.RefreshUnavailableReason);
    }

    [TestMethod]
    public async Task GetCapabilitiesAsync_WhenInforVisualIsNotConfigured_DisablesNothingAsync()
    {
        // Not configured is "unknown", not "unreachable": disabling refresh here would hide a provisioning gap
        // behind a silent no-op, and the refresh cycle already reports an unreachable source as a normal skip.
        var probe = CreateProbe(visualConfigured: false, visualReachable: false);

        var capabilities = await probe.GetCapabilitiesAsync();

        Assert.IsTrue(capabilities.IsRefreshAvailable);
    }

    [TestMethod]
    public async Task GetCapabilitiesAsync_DisablesOnlyTheStoresThatCouldNotBeReachedAsync()
    {
        var probe = CreateProbe(
            storeResults: new Dictionary<BackupStore, StoreProbeResult>
            {
                [BackupStore.MtmWaitlist] = StoreProbeResult.Reachable,
                [BackupStore.MtmWipApplicationWinforms] = StoreProbeResult.Unreachable("MySQL did not accept a connection to 'mtm_wip_application_winforms' from this machine."),
                [BackupStore.MtmReceivingApplication] = StoreProbeResult.NotConfigured,
                [BackupStore.MtmMock] = StoreProbeResult.Reachable
            });

        var capabilities = await probe.GetCapabilitiesAsync();

        Assert.IsTrue(capabilities.IsStoreAvailable(BackupStore.MtmWaitlist), "A reachable store's backup must stay enabled.");
        Assert.IsFalse(capabilities.IsStoreAvailable(BackupStore.MtmWipApplicationWinforms), "One unreachable store must be disabled.");
        Assert.IsTrue(capabilities.IsStoreAvailable(BackupStore.MtmReceivingApplication), "A store with no connection string is unknown, not disabled.");
        Assert.IsTrue(capabilities.IsStoreAvailable(BackupStore.MtmMock));
        StringAssert.Contains(capabilities.StoreUnavailableReason(BackupStore.MtmWipApplicationWinforms)!, "mtm_wip_application_winforms");
        Assert.IsNull(capabilities.StoreUnavailableReason(BackupStore.MtmWaitlist));
    }

    [TestMethod]
    public async Task GetCapabilitiesAsync_WithinTheCacheLifetime_MeasuresOnceAsync()
    {
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 9, 12, 12, 0, 0, TimeSpan.Zero));
        var counter = new CountingStoreProbe();
        var probe = CreateProbe(clock: clock, storeProbe: counter, cacheLifetime: TimeSpan.FromMinutes(1));

        await probe.GetCapabilitiesAsync();
        var afterFirst = counter.Count;

        clock.Advance(TimeSpan.FromSeconds(30));
        await probe.GetCapabilitiesAsync();

        Assert.AreEqual(afterFirst, counter.Count, "A cached verdict must be reused inside its lifetime.");
    }

    [TestMethod]
    public async Task GetCapabilitiesAsync_AfterTheCacheLifetime_MeasuresAgainSoAccessCanReturnAsync()
    {
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 9, 12, 12, 0, 0, TimeSpan.Zero));
        var visual = new StubVisualConnectivityProbe { IsReachable = false };
        var probe = CreateProbe(
            clock: clock,
            cacheLifetime: TimeSpan.FromMinutes(1),
            visualProbe: visual);

        var before = await probe.GetCapabilitiesAsync();
        Assert.IsFalse(before.IsRefreshAvailable);

        // The machine regains access. Nothing restarts the service; the next measurement must see it.
        visual.IsReachable = true;
        clock.Advance(TimeSpan.FromMinutes(2));

        var after = await probe.GetCapabilitiesAsync();

        Assert.IsTrue(after.IsRefreshAvailable, "A measurement after the cache lifetime must pick up restored access.");
    }

    [TestMethod]
    public async Task GetCapabilitiesAsync_WhenAskedToReprobe_IgnoresTheCachedVerdictAsync()
    {
        var counter = new CountingStoreProbe();
        var probe = CreateProbe(storeProbe: counter);
        await probe.GetCapabilitiesAsync();
        var afterFirst = counter.Count;

        await probe.GetCapabilitiesAsync(reprobe: true);

        Assert.IsTrue(counter.Count > afterFirst, "reprobe: true must take a fresh measurement.");
    }

    [TestMethod]
    public async Task GetCapabilitiesAsync_WhenTheProbeItselfFails_DisablesNothingAsync()
    {
        // A probe that cannot run is not evidence of unreachability, so no work is disabled by it.
        var probe = CreateProbe(storeProbe: new ThrowingStoreProbe(), visualProbe: new ThrowingVisualProbe());

        var capabilities = await probe.GetCapabilitiesAsync();

        Assert.IsTrue(capabilities.IsRefreshAvailable, "A failed Visual probe must not disable refresh.");
        Assert.IsTrue(
            capabilities.IsStoreAvailable(BackupStore.MtmWaitlist),
            "A failed store probe must not disable that store's backup.");
    }

    [TestMethod]
    public void Current_BeforeAnyMeasurement_DisablesNothing()
    {
        var probe = CreateProbe();

        Assert.IsTrue(probe.Current.IsRefreshAvailable);
        Assert.IsTrue(probe.Current.IsStoreAvailable(BackupStore.MtmMock));
    }

    private static ServiceCapabilityProbe CreateProbe(
        bool visualConfigured = true,
        bool visualReachable = true,
        IVisualConnectivityProbe? visualProbe = null,
        IMySqlStoreConnectivityProbe? storeProbe = null,
        IReadOnlyDictionary<BackupStore, StoreProbeResult>? storeResults = null,
        TestTimeProvider? clock = null,
        TimeSpan? cacheLifetime = null) =>
        new(
            new StubVisualConnectionStringProvider(visualConfigured),
            visualProbe ?? new StubVisualConnectivityProbe { IsReachable = visualReachable },
            storeProbe ?? new StubStoreConnectivityProbe(storeResults),
            NullLogger<ServiceCapabilityProbe>.Instance,
            clock ?? new TestTimeProvider(DateTimeOffset.UtcNow),
            cacheLifetime: cacheLifetime ?? TimeSpan.FromMinutes(1));

    /// <summary>Counts probes, so the cache can be asserted instead of assumed.</summary>
    private sealed class CountingStoreProbe : IMySqlStoreConnectivityProbe
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public Task<StoreProbeResult> ProbeAsync(BackupStore store, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _count);
            return Task.FromResult(StoreProbeResult.Reachable);
        }
    }

    private sealed class StubVisualConnectionStringProvider : IVisualConnectionStringProvider
    {
        private readonly bool _configured;

        public StubVisualConnectionStringProvider(bool configured) => _configured = configured;

        public string Resolve() => _configured ? "Server=VISUAL;Database=MTMFG;User Id=shop;Password=x;" : string.Empty;
    }

    private class StubVisualConnectivityProbe : IVisualConnectivityProbe
    {
        public bool IsReachable { get; set; } = true;

        public virtual Task<bool> ProbeAsync(CancellationToken cancellationToken = default) => Task.FromResult(IsReachable);
    }

    private sealed class ThrowingVisualProbe : StubVisualConnectivityProbe
    {
        public override Task<bool> ProbeAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The probe could not run.");
    }

    private class StubStoreConnectivityProbe : IMySqlStoreConnectivityProbe
    {
        private readonly IReadOnlyDictionary<BackupStore, StoreProbeResult>? _results;

        public StubStoreConnectivityProbe(IReadOnlyDictionary<BackupStore, StoreProbeResult>? results) => _results = results;

        public virtual Task<StoreProbeResult> ProbeAsync(BackupStore store, CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _results is not null && _results.TryGetValue(store, out var result)
                    ? result
                    : StoreProbeResult.Reachable);
    }

    private sealed class ThrowingStoreProbe : StubStoreConnectivityProbe
    {
        public ThrowingStoreProbe()
            : base(results: null)
        {
        }

        public override Task<StoreProbeResult> ProbeAsync(BackupStore store, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The probe could not run.");
    }

    /// <summary>A clock the test advances by hand; nothing here waits on wall time.</summary>
    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public TestTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan delta) => _utcNow += delta;
    }
}
