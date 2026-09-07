using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class MockModeSummaryProviderTests
{
    [TestMethod]
    public void SourceDisplayName_MapsKnownSources()
    {
        Assert.AreEqual("Infor Visual", MockModeSummaryProvider.SourceDisplayName(ConnectionSource.InforVisual));
        Assert.AreEqual("Receiving", MockModeSummaryProvider.SourceDisplayName(ConnectionSource.Receiving));
    }

    [TestMethod]
    public void Describe_Forced_NamesSourceAndUnreachable()
    {
        var d = new MockRoutingDecision { Source = ConnectionSource.Receiving, UseMockData = true, IsAutoForced = true };
        Assert.AreEqual("Receiving is unreachable — mock data is forced on.", MockModeSummaryProvider.Describe(d));
    }

    [TestMethod]
    public void Describe_MockVsLive()
    {
        var mock = new MockRoutingDecision { Source = ConnectionSource.InforVisual, UseMockData = true, IsAutoForced = false };
        var live = new MockRoutingDecision { Source = ConnectionSource.InforVisual, UseMockData = false, IsAutoForced = false };

        Assert.AreEqual("Infor Visual is using mock data.", MockModeSummaryProvider.Describe(mock));
        Assert.AreEqual("Infor Visual is using live data.", MockModeSummaryProvider.Describe(live));
    }

    [TestMethod]
    public void ToastMessage_NamesSourcePerKind()
    {
        Assert.AreEqual(
            "Receiving unreachable — switched to mock data.",
            MockModeSummaryProvider.ToastMessage(new MockModeChange { Source = ConnectionSource.Receiving, Kind = MockModeChangeKind.ForcedOn }));

        Assert.AreEqual(
            "Infor Visual is back online — live data restored.",
            MockModeSummaryProvider.ToastMessage(new MockModeChange { Source = ConnectionSource.InforVisual, Kind = MockModeChangeKind.Recovered, UseMockDataAfter = false }));

        Assert.AreEqual(
            "Infor Visual is back online — mock data stays on by configuration.",
            MockModeSummaryProvider.ToastMessage(new MockModeChange { Source = ConnectionSource.InforVisual, Kind = MockModeChangeKind.Recovered, UseMockDataAfter = true }));

        Assert.AreEqual(
            "Receiving is now using live data.",
            MockModeSummaryProvider.ToastMessage(new MockModeChange { Source = ConnectionSource.Receiving, Kind = MockModeChangeKind.TurnedOff }));
    }
}
