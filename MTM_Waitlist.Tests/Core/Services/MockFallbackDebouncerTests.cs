using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class MockFallbackDebouncerTests
{
    [TestMethod]
    public void SingleBlip_DoesNotFlipStableStatus()
    {
        var debouncer = new MockFallbackDebouncer(threshold: 2);

        // Stable starts Unknown; two Connected observations establish Connected.
        Assert.AreEqual(ConnectionHealthStatus.Unknown, debouncer.Observe(ConnectionSource.Receiving, ConnectionHealthStatus.Unknown));
        Assert.AreEqual(ConnectionHealthStatus.Unknown, debouncer.Observe(ConnectionSource.Receiving, ConnectionHealthStatus.Connected));
        Assert.AreEqual(ConnectionHealthStatus.Connected, debouncer.Observe(ConnectionSource.Receiving, ConnectionHealthStatus.Connected));

        // A single Unreachable blip must NOT flip the stable Connected status.
        Assert.AreEqual(ConnectionHealthStatus.Connected, debouncer.Observe(ConnectionSource.Receiving, ConnectionHealthStatus.Unreachable));

        // Back to Connected — still Connected, no thrash.
        Assert.AreEqual(ConnectionHealthStatus.Connected, debouncer.Observe(ConnectionSource.Receiving, ConnectionHealthStatus.Connected));
    }

    [TestMethod]
    public void PersistentFailures_FlipToUnreachable_OnlyAfterThreshold()
    {
        var debouncer = new MockFallbackDebouncer(threshold: 2);
        debouncer.Observe(ConnectionSource.Receiving, ConnectionHealthStatus.Unknown);
        debouncer.Observe(ConnectionSource.Receiving, ConnectionHealthStatus.Connected);
        debouncer.Observe(ConnectionSource.Receiving, ConnectionHealthStatus.Connected); // Connected stable

        // First unreachable: below threshold -> stays Connected.
        Assert.AreEqual(ConnectionHealthStatus.Connected, debouncer.Observe(ConnectionSource.Receiving, ConnectionHealthStatus.Unreachable));
        // Second consecutive unreachable: threshold met -> flips.
        Assert.AreEqual(ConnectionHealthStatus.Unreachable, debouncer.Observe(ConnectionSource.Receiving, ConnectionHealthStatus.Unreachable));
    }

    [TestMethod]
    public void Recovery_RequiresThresholdOfConnectedBeforeDisablingForce()
    {
        var debouncer = new MockFallbackDebouncer(threshold: 2);
        // Establish Unreachable as stable.
        debouncer.Observe(ConnectionSource.Receiving, ConnectionHealthStatus.Unknown);
        debouncer.Observe(ConnectionSource.Receiving, ConnectionHealthStatus.Unreachable);
        debouncer.Observe(ConnectionSource.Receiving, ConnectionHealthStatus.Unreachable);

        // First recovery probe below threshold -> still Unreachable.
        Assert.AreEqual(ConnectionHealthStatus.Unreachable, debouncer.Observe(ConnectionSource.Receiving, ConnectionHealthStatus.Connected));
        // Second consecutive Connected -> recovered.
        Assert.AreEqual(ConnectionHealthStatus.Connected, debouncer.Observe(ConnectionSource.Receiving, ConnectionHealthStatus.Connected));
    }

    [TestMethod]
    public void SourcesAreTrackedIndependently()
    {
        var debouncer = new MockFallbackDebouncer(threshold: 1);
        debouncer.Observe(ConnectionSource.InforVisual, ConnectionHealthStatus.Connected);
        debouncer.Observe(ConnectionSource.Receiving, ConnectionHealthStatus.Unreachable);

        Assert.AreEqual(ConnectionHealthStatus.Connected, debouncer.Observe(ConnectionSource.InforVisual, ConnectionHealthStatus.Connected));
        Assert.AreEqual(ConnectionHealthStatus.Unreachable, debouncer.Observe(ConnectionSource.Receiving, ConnectionHealthStatus.Unreachable));
    }

    [TestMethod]
    public void Reset_ClearsState()
    {
        var debouncer = new MockFallbackDebouncer(threshold: 2);
        debouncer.Observe(ConnectionSource.Receiving, ConnectionHealthStatus.Connected);
        debouncer.Observe(ConnectionSource.Receiving, ConnectionHealthStatus.Connected); // Connected stable
        Assert.AreEqual(ConnectionHealthStatus.Connected, debouncer.Observe(ConnectionSource.Receiving, ConnectionHealthStatus.Connected));

        debouncer.Reset();

        // After reset, stable is Unknown until the threshold is met again.
        Assert.AreEqual(ConnectionHealthStatus.Unknown, debouncer.Observe(ConnectionSource.Receiving, ConnectionHealthStatus.Connected));
        Assert.AreEqual(ConnectionHealthStatus.Connected, debouncer.Observe(ConnectionSource.Receiving, ConnectionHealthStatus.Connected));
    }
}
