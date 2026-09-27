using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The machine facts contract's own promises (`contracts/machine-configuration-contract.md`; FR-015): this
/// machine's name and hardware address are read from the machine, the registry row is read by that pair through
/// the one existing lookup rather than a second way of finding a machine, and an unreadable hardware identity is
/// admitted instead of being reported as a failed check.
/// </summary>
/// <remarks>
/// <para>
/// The hardware address is read from the real machine in the constructor and cannot be injected, so a test that
/// needs a readable one says so and stands down on a build host that has no usable adapter, rather than asserting
/// a fact about the host it happens to run on. On a machine with a network adapter every one of them is
/// conclusive; without one they report themselves instead of failing.
/// </para>
/// <para>
/// What is pinned here is the seam rather than the machine: which procedure is called, against which database,
/// with which pair of values, and what an empty answer means.
/// </para>
/// </remarks>
[TestClass]
public sealed class MachineFactsServiceTests
{
    private const string LookupProcedure = "sp_core_computers_registry_lookup_by_name_mac_get";

    /// <summary>The form the store holds a hardware address in: lower case, hyphen separated, six bytes.</summary>
    private static readonly Regex s_storesForm = new("^[0-9a-f]{2}(-[0-9a-f]{2}){5}$", RegexOptions.Compiled);

    [TestMethod]
    public void Hostname_IsThisMachinesName()
    {
        var service = new MachineFactsService(new StubMySqlHelperServer([]));

        Assert.AreEqual(Environment.MachineName.Trim(), service.Hostname);
        Assert.AreNotEqual(0, service.Hostname.Length);
    }

    [TestMethod]
    public void HardwareIdentityReadable_AgreesWithTheAddress()
    {
        var service = new MachineFactsService(new StubMySqlHelperServer([]));

        // The two answers are one fact said twice, so they must never disagree.
        Assert.AreEqual(!string.IsNullOrWhiteSpace(service.MacAddress), service.HardwareIdentityReadable);
    }

    [TestMethod]
    public void MacAddress_WhenReadable_IsHeldInTheStoresForm()
    {
        var service = new MachineFactsService(new StubMySqlHelperServer([]));

        if (!service.HardwareIdentityReadable)
        {
            Assert.Inconclusive("This machine reports no six-byte hardware address, so there is none to check.");
        }

        // Lower case and hyphen separated is what the lookup matches on, so an address in any other form would
        // never find the machine's own registry row.
        Assert.IsTrue(s_storesForm.IsMatch(service.MacAddress!), $"Unexpected address form: {service.MacAddress}");
    }

    [TestMethod]
    public void RegisteredComputer_StartsUnknown()
    {
        var service = new MachineFactsService(new StubMySqlHelperServer([]));

        Assert.IsNull(service.RegisteredComputer);
        Assert.IsFalse(service.IsRegistered);
    }

    [TestMethod]
    public async Task RefreshAsync_ReadsTheRegistryByThePairTheMachinePresents()
    {
        var stub = new StubMySqlHelperServer([RegistryRow(isRegistered: 1L)]);
        var service = new MachineFactsService(stub);

        if (!service.HardwareIdentityReadable)
        {
            Assert.Inconclusive("This machine reports no hardware address, so the registry read cannot be exercised.");
        }

        var registered = await service.RefreshAsync();

        Assert.IsTrue(registered);
        Assert.AreEqual(LookupProcedure, stub.LastStoredProcedureName);
        Assert.AreEqual(MySqlDatabaseTarget.MtmWaitlist, stub.LastDatabaseTarget);
        Assert.AreEqual(service.Hostname, stub.LastParameters["p_computer_name"]);
        Assert.AreEqual(service.MacAddress, stub.LastParameters["p_mac_address_normalized"]);
        Assert.AreEqual(1, stub.QueryCallCount);
        Assert.IsTrue(service.IsRegistered);
        Assert.AreEqual("Shop floor station", service.RegisteredComputer!.DisplayName);
    }

    [TestMethod]
    public async Task RefreshAsync_RegisteredFlagSurfacedAsBoolean_CountsAsRegistered()
    {
        // The driver surfaces a TINYINT(1) column as a bool, and reading it as a number would throw rather than
        // answer, so the registered flag is pinned in that form too.
        var stub = new StubMySqlHelperServer([RegistryRow(isRegistered: true)]);
        var service = new MachineFactsService(stub);

        if (!service.HardwareIdentityReadable)
        {
            Assert.Inconclusive("This machine reports no hardware address, so the registry read cannot be exercised.");
        }

        Assert.IsTrue(await service.RefreshAsync());
        Assert.IsTrue(service.IsRegistered);
    }

    [TestMethod]
    public async Task RefreshAsync_WhenTheStoreHoldsNoRow_LeavesTheMachineUnregistered()
    {
        var stub = new StubMySqlHelperServer([]);
        var service = new MachineFactsService(stub);

        if (!service.HardwareIdentityReadable)
        {
            Assert.Inconclusive("This machine reports no hardware address, so the registry read cannot be exercised.");
        }

        var registered = await service.RefreshAsync();

        // The read was made and the store answered nothing. That is a different fact from a read that could not
        // be made at all, and neither is an error: the machine is simply not registered.
        Assert.AreEqual(1, stub.QueryCallCount);
        Assert.IsFalse(registered);
        Assert.IsNull(service.RegisteredComputer);
        Assert.IsFalse(service.IsRegistered);
    }

    [TestMethod]
    public void Apply_ReplacesTheResolvedRow()
    {
        var service = new MachineFactsService(new StubMySqlHelperServer([]));

        service.Apply(new ComputerRecord { Id = 7L, ComputerName = "WS-1", IsRegistered = true });

        Assert.IsTrue(service.IsRegistered);
        Assert.AreEqual(7L, service.RegisteredComputer!.Id);

        service.Apply(null);

        Assert.IsFalse(service.IsRegistered);
        Assert.IsNull(service.RegisteredComputer);
    }

    /// <summary>One registry row as the lookup returns it, in the column names the mapping reads.</summary>
    private static Dictionary<string, object?> RegistryRow(object isRegistered) => new()
    {
        ["id"] = 7L,
        ["computer_name"] = Environment.MachineName.Trim(),
        ["display_name"] = "Shop floor station",
        ["description"] = "a row the test owns",
        ["mac_address_normalized"] = "aa-bb-cc-dd-ee-ff",
        ["is_registered"] = isRegistered,
    };

    /// <summary>
    /// Records what it was asked for and answers with the rows it was built with, so the seam can be read back
    /// without a database.
    /// </summary>
    private sealed class StubMySqlHelperServer : IMySqlHelperServer
    {
        private readonly IReadOnlyList<Dictionary<string, object?>> _rows;

        public StubMySqlHelperServer(IReadOnlyList<Dictionary<string, object?>> rows) => _rows = rows;

        public int QueryCallCount { get; private set; }

        public string? LastStoredProcedureName { get; private set; }

        public IReadOnlyDictionary<string, object?> LastParameters { get; private set; } =
            new Dictionary<string, object?>();

        public MySqlDatabaseTarget? LastDatabaseTarget { get; private set; }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            QueryCallCount++;
            LastStoredProcedureName = storedProcedureName;
            LastParameters = parameters;
            LastDatabaseTarget = databaseTarget;

            return Task.FromResult(_rows);
        }

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_rows);

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(1);
    }
}
