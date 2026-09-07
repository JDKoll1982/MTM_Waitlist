using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class MockRoutingServiceTests
{
    private readonly MockRoutingService _service = new();

    private static ConnectionHealthState Health(ConnectionHealthStatus status) =>
        new() { Source = ConnectionSource.Receiving, Status = status };

    private static MockSettingState Central(bool? enabled)
    {
        if (enabled is null)
        {
            return new MockSettingState { SettingKey = "Feature.RecvMockData", IsPresent = false, IsMockEnabled = false };
        }

        return new MockSettingState { SettingKey = "Feature.RecvMockData", IsPresent = true, IsMockEnabled = enabled.Value };
    }

    [TestMethod]
    public void Resolve_SourceUnreachable_ForcesMockOn_AndAutoForced()
    {
        var decision = _service.Resolve(
            ConnectionSource.Receiving,
            localManualOverride: false,
            Central(false),
            Health(ConnectionHealthStatus.Unreachable));

        Assert.IsTrue(decision.UseMockData);
        Assert.IsTrue(decision.IsAutoForced);
        Assert.AreEqual(MockRoutingReason.SourceUnreachable, decision.Reason);
    }

    [TestMethod]
    public void Resolve_SourceUnreachable_OverridesLocalManualOff()
    {
        // Even if the user turned mock off locally, an unreachable source must still force mock on.
        var decision = _service.Resolve(
            ConnectionSource.Receiving,
            localManualOverride: false,
            Central(false),
            Health(ConnectionHealthStatus.Unreachable));

        Assert.IsTrue(decision.UseMockData);
    }

    [TestMethod]
    public void Resolve_Reachable_LocalOverrideTrue_Wins()
    {
        var decision = _service.Resolve(
            ConnectionSource.Receiving,
            localManualOverride: true,
            Central(false),
            Health(ConnectionHealthStatus.Connected));

        Assert.IsTrue(decision.UseMockData);
        Assert.IsFalse(decision.IsAutoForced);
        Assert.AreEqual(MockRoutingReason.ManualOverride, decision.Reason);
    }

    [TestMethod]
    public void Resolve_Reachable_LocalOverrideFalse_Wins_OverCentralTrue()
    {
        var decision = _service.Resolve(
            ConnectionSource.Receiving,
            localManualOverride: false,
            Central(true),
            Health(ConnectionHealthStatus.Connected));

        Assert.IsFalse(decision.UseMockData);
        Assert.AreEqual(MockRoutingReason.ManualOverride, decision.Reason);
    }

    [TestMethod]
    public void Resolve_Reachable_NoLocal_CentralTrue_Governs()
    {
        var decision = _service.Resolve(
            ConnectionSource.Receiving,
            localManualOverride: null,
            Central(true),
            Health(ConnectionHealthStatus.Connected));

        Assert.IsTrue(decision.UseMockData);
        Assert.AreEqual(MockRoutingReason.CentralConfig, decision.Reason);
    }

    [TestMethod]
    public void Resolve_Reachable_NoLocal_CentralAbsent_DefaultsOff()
    {
        var decision = _service.Resolve(
            ConnectionSource.Receiving,
            localManualOverride: null,
            Central(null),
            Health(ConnectionHealthStatus.Connected));

        Assert.IsFalse(decision.UseMockData);
        Assert.AreEqual(MockRoutingReason.DefaultOff, decision.Reason);
    }

    [TestMethod]
    public void Resolve_UnknownHealth_TreatedAsReachable()
    {
        // Before the first health check, prefer configured settings rather than forcing mock.
        var decision = _service.Resolve(
            ConnectionSource.Receiving,
            localManualOverride: null,
            Central(true),
            Health(ConnectionHealthStatus.Unknown));

        Assert.IsTrue(decision.UseMockData);
        Assert.IsFalse(decision.IsAutoForced);
    }
}
