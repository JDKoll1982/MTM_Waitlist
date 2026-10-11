using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Services;

namespace MTM_Waitlist.Tests.Module_Mock;

/// <summary>
/// Verifies the reachability detector's hysteresis: two consecutive failures are required to accept
/// that Infor Visual is down, while a single success returns to live immediately — and the state can
/// only ever change as a result of a probe (FR-002, FR-003).
/// </summary>
[TestClass]
public sealed class VisualReachabilityDetectorTests
{
    [TestMethod]
    public async Task Service_VisualReachabilityDetectorTests_Probe_FirstSuccessGoesLive()
    {
        var detector = new VisualReachabilityDetector(new FakeVisualConnectivityProbe(true));

        await detector.ProbeAsync();

        Assert.AreEqual(VisualReadStatus.Live, detector.Current);
    }

    [TestMethod]
    public async Task Service_VisualReachabilityDetectorTests_Probe_SingleFailureStaysUnknownSoOneBlipDoesNotFlipTheApp()
    {
        var detector = new VisualReachabilityDetector(new FakeVisualConnectivityProbe(false));

        await detector.ProbeAsync();

        Assert.AreEqual(VisualReadStatus.Unknown, detector.Current);
    }

    [TestMethod]
    public async Task Service_VisualReachabilityDetectorTests_Probe_TwoConsecutiveFailuresGoesCached()
    {
        var detector = new VisualReachabilityDetector(new FakeVisualConnectivityProbe(false, false));

        await detector.ProbeAsync();
        await detector.ProbeAsync();

        Assert.AreEqual(VisualReadStatus.Cached, detector.Current);
    }

    [TestMethod]
    public async Task Service_VisualReachabilityDetectorTests_Probe_OneSuccessWhileCachedReturnsLiveImmediately()
    {
        var detector = new VisualReachabilityDetector(new FakeVisualConnectivityProbe(false, false, true));

        await detector.ProbeAsync();
        await detector.ProbeAsync();
        Assert.AreEqual(VisualReadStatus.Cached, detector.Current);

        await detector.ProbeAsync();

        Assert.AreEqual(VisualReadStatus.Live, detector.Current);
    }

    [TestMethod]
    public async Task Service_VisualReachabilityDetectorTests_Probe_SuccessResetsTheFailureCount()
    {
        // A success between two failures means they are not consecutive, so the state must stay Live.
        var detector = new VisualReachabilityDetector(new FakeVisualConnectivityProbe(true, false, true, false));

        await detector.ProbeAsync();
        await detector.ProbeAsync();

        Assert.AreEqual(VisualReadStatus.Live, detector.Current);
    }

    [TestMethod]
    public async Task Service_VisualReachabilityDetectorTests_StateChanged_IsRaisedOnlyWhenTheStateActuallyChanges()
    {
        var detector = new VisualReachabilityDetector(new FakeVisualConnectivityProbe(false, false, false));
        var transitions = new List<VisualReadStatus>();
        detector.StateChanged += (_, status) => transitions.Add(status);

        await detector.ProbeAsync();
        await detector.ProbeAsync();
        await detector.ProbeAsync();

        CollectionAssert.AreEqual(new[] { VisualReadStatus.Cached }, transitions);
    }

    [TestMethod]
    public void Service_VisualReachabilityDetectorTests_Current_StartsUnknown()
    {
        var detector = new VisualReachabilityDetector(new FakeVisualConnectivityProbe(true));

        Assert.AreEqual(VisualReadStatus.Unknown, detector.Current);
    }
}
