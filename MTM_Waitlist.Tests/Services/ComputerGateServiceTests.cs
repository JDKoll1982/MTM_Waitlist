using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Services;

[TestClass]
public sealed class ComputerGateServiceTests
{
    [TestMethod]
    public async Task CheckAsync_WhenMacMissing_ReturnsSkippedNoMacAsync()
    {
        var startupState = new StartupState
        {
            HostnameNormalized = "johnspc",
            MacAddressNormalized = string.Empty
        };
        var service = new ComputerGateService(new FakeComputerRegistryService(), startupState);

        var result = await service.CheckAsync();

        Assert.AreEqual(ComputerGateStatus.SkippedNoMac, result.Status);
        Assert.IsNull(result.ExistingComputer);
    }

    [TestMethod]
    public async Task CheckAsync_WhenCompositeMatch_ReturnsRegisteredAsync()
    {
        var startupState = new StartupState
        {
            HostnameNormalized = "johnspc",
            MacAddressNormalized = "d8-43-ae-47-d0-d6"
        };
        var existing = new ComputerRecord { Id = 1, ComputerName = "johnspc", DisplayName = "John's Computer" };
        var registry = new FakeComputerRegistryService { CompositeResult = existing };
        var service = new ComputerGateService(registry, startupState);

        var result = await service.CheckAsync();

        Assert.AreEqual(ComputerGateStatus.Registered, result.Status);
        Assert.AreSame(existing, result.ExistingComputer);
        Assert.AreEqual(0, registry.LookupByMacCount);
    }

    [TestMethod]
    public async Task CheckAsync_WhenCompositeMissingButMacMatch_ReturnsRenamedMachineAsync()
    {
        var startupState = new StartupState
        {
            HostnameNormalized = "new-host",
            MacAddressNormalized = "d8-43-ae-47-d0-d6"
        };
        var byMac = new ComputerRecord { Id = 1, ComputerName = "old-host", DisplayName = "Old Name" };
        var registry = new FakeComputerRegistryService { CompositeResult = null, ByMacResult = byMac };
        var service = new ComputerGateService(registry, startupState);

        var result = await service.CheckAsync();

        Assert.AreEqual(ComputerGateStatus.RenamedMachine, result.Status);
        Assert.AreSame(byMac, result.ExistingComputer);
    }

    [TestMethod]
    public async Task CheckAsync_WhenNoMatch_ReturnsMissingAsync()
    {
        var startupState = new StartupState
        {
            HostnameNormalized = "johnspc",
            MacAddressNormalized = "d8-43-ae-47-d0-d6"
        };
        var registry = new FakeComputerRegistryService { CompositeResult = null, ByMacResult = null };
        var service = new ComputerGateService(registry, startupState);

        var result = await service.CheckAsync();

        Assert.AreEqual(ComputerGateStatus.Missing, result.Status);
    }

    [TestMethod]
    public async Task CheckAsync_WhenLookupThrows_ReturnsDatabaseUnavailableAsync()
    {
        var startupState = new StartupState
        {
            HostnameNormalized = "johnspc",
            MacAddressNormalized = "d8-43-ae-47-d0-d6"
        };
        var registry = new FakeComputerRegistryService { LookupException = new InvalidOperationException("db down") };
        var service = new ComputerGateService(registry, startupState);

        var result = await service.CheckAsync();

        Assert.AreEqual(ComputerGateStatus.DatabaseUnavailable, result.Status);
    }

    private sealed class FakeComputerRegistryService : IComputerRegistryService
    {
        public ComputerRecord? CompositeResult { get; set; }

        public ComputerRecord? ByMacResult { get; set; }

        public Exception? LookupException { get; set; }

        public int LookupByMacCount { get; private set; }

        public Task<ComputerRecord?> LookupComputerAsync(string computerName, string macAddressNormalized, CancellationToken cancellationToken = default)
        {
            if (LookupException is not null)
            {
                throw LookupException;
            }

            return Task.FromResult(CompositeResult);
        }

        public Task<ComputerRecord?> LookupComputerByMacAsync(string macAddressNormalized, CancellationToken cancellationToken = default)
        {
            LookupByMacCount++;
            return Task.FromResult(ByMacResult);
        }

        public Task<ComputerRecord> UpsertComputerAsync(string computerName, string hostnameNormalized, string macAddressNormalized, string displayName, string? description, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ComputerRecord> UpdateComputerByMacAsync(string macAddressNormalized, string newComputerName, string hostnameNormalized, string displayName, string? description, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
