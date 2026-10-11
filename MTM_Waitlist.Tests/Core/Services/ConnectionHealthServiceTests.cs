using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class ConnectionHealthServiceTests
{
    [TestMethod]
    public async Task Service_ConnectionHealthServiceTests_CheckAsync_UnconfiguredSourceReturnsUnreachable()
    {
        var service = new ConnectionHealthService(new StubProvider());

        var state = await service.CheckAsync(ConnectionSource.InforVisual);

        Assert.AreEqual(ConnectionHealthStatus.Unreachable, state.Status);
        Assert.IsTrue(state.IsUnreachable);
        Assert.AreEqual(ConnectionSource.InforVisual, state.Source);
    }

    [TestMethod]
    public void Service_ConnectionHealthServiceTests_GetLastKnown_BeforeAnyCheckReturnsUnknown()
    {
        var service = new ConnectionHealthService(new StubProvider());

        var state = service.GetLastKnown(ConnectionSource.Receiving);

        Assert.AreEqual(ConnectionHealthStatus.Unknown, state.Status);
    }

    [TestMethod]
    public async Task Service_ConnectionHealthServiceTests_CheckAllAsync_ReturnsBothSourcesWithUnreachableWhenNotConfigured()
    {
        var service = new ConnectionHealthService(new StubProvider());

        var states = await service.CheckAllAsync();

        Assert.AreEqual(2, states.Count);
        Assert.AreEqual(ConnectionHealthStatus.Unreachable, states[ConnectionSource.InforVisual].Status);
        Assert.AreEqual(ConnectionHealthStatus.Unreachable, states[ConnectionSource.Receiving].Status);
    }

    private sealed class StubProvider : IExternalConnectionInfoProvider
    {
        public bool IsSqlServer(ConnectionSource source) => source == ConnectionSource.InforVisual;

        public string? ResolveConnectionString(ConnectionSource source) => null;
    }
}
