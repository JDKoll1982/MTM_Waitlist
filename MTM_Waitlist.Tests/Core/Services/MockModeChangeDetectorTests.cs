using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class MockModeChangeDetectorTests
{
    private static MockRoutingDecision Decision(bool useMock, bool autoForced, MockRoutingReason reason) =>
        new() { Source = ConnectionSource.Receiving, UseMockData = useMock, IsAutoForced = autoForced, Reason = reason };

    [TestMethod]
    public void Detect_FirstObservation_IsNone_NoNotify()
    {
        var change = MockModeChangeDetector.Detect(
            ConnectionSource.Receiving,
            previous: null,
            Decision(true, true, MockRoutingReason.SourceUnreachable));

        Assert.AreEqual(MockModeChangeKind.None, change.Kind);
        Assert.IsFalse(change.ShouldNotify);
    }

    [TestMethod]
    public void Detect_HealthyToUnreachable_IsForcedOn()
    {
        var change = MockModeChangeDetector.Detect(
            ConnectionSource.Receiving,
            Decision(false, false, MockRoutingReason.DefaultOff),
            Decision(true, true, MockRoutingReason.SourceUnreachable));

        Assert.AreEqual(MockModeChangeKind.ForcedOn, change.Kind);
        Assert.IsTrue(change.ShouldNotify);
        Assert.IsTrue(change.IsAutoForcedAfter);
    }

    [TestMethod]
    public void Detect_UnreachableToRecovered_IsRecovered()
    {
        var change = MockModeChangeDetector.Detect(
            ConnectionSource.Receiving,
            Decision(true, true, MockRoutingReason.SourceUnreachable),
            Decision(false, false, MockRoutingReason.DefaultOff));

        Assert.AreEqual(MockModeChangeKind.Recovered, change.Kind);
        Assert.IsTrue(change.ShouldNotify);
        Assert.IsFalse(change.IsAutoForcedAfter);
    }

    [TestMethod]
    public void Detect_RecoveredButConfigKeepsMockOn_IsRecoveredWithMockOn()
    {
        var change = MockModeChangeDetector.Detect(
            ConnectionSource.Receiving,
            Decision(true, true, MockRoutingReason.SourceUnreachable),
            Decision(true, false, MockRoutingReason.CentralConfig));

        Assert.AreEqual(MockModeChangeKind.Recovered, change.Kind);
        Assert.IsTrue(change.UseMockDataAfter);
        Assert.IsFalse(change.IsAutoForcedAfter);
    }

    [TestMethod]
    public void Detect_StillUnreachable_IsStillForced_NoNotify()
    {
        var change = MockModeChangeDetector.Detect(
            ConnectionSource.Receiving,
            Decision(true, true, MockRoutingReason.SourceUnreachable),
            Decision(true, true, MockRoutingReason.SourceUnreachable));

        Assert.AreEqual(MockModeChangeKind.StillForced, change.Kind);
        Assert.IsFalse(change.ShouldNotify);
    }

    [TestMethod]
    public void Detect_ReachableToggleOn_IsTurnedOn()
    {
        var change = MockModeChangeDetector.Detect(
            ConnectionSource.Receiving,
            Decision(false, false, MockRoutingReason.DefaultOff),
            Decision(true, false, MockRoutingReason.ManualOverride));

        Assert.AreEqual(MockModeChangeKind.TurnedOn, change.Kind);
        Assert.IsTrue(change.ShouldNotify);
    }

    [TestMethod]
    public void Detect_ReachableToggleOff_IsTurnedOff()
    {
        var change = MockModeChangeDetector.Detect(
            ConnectionSource.Receiving,
            Decision(true, false, MockRoutingReason.CentralConfig),
            Decision(false, false, MockRoutingReason.DefaultOff));

        Assert.AreEqual(MockModeChangeKind.TurnedOff, change.Kind);
        Assert.IsTrue(change.ShouldNotify);
    }

    [TestMethod]
    public void Detect_NoEffectiveChange_IsNone()
    {
        var change = MockModeChangeDetector.Detect(
            ConnectionSource.Receiving,
            Decision(false, false, MockRoutingReason.DefaultOff),
            Decision(false, false, MockRoutingReason.DefaultOff));

        Assert.AreEqual(MockModeChangeKind.None, change.Kind);
        Assert.IsFalse(change.ShouldNotify);
    }
}
