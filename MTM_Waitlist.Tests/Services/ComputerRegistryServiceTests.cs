using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Services;

[TestClass]
public sealed class ComputerRegistryServiceTests
{
    [TestMethod]
    public async Task LookupComputerAsync_WhenRowFound_MapsRecordAsync()
    {
        var helper = new FakeMySqlHelperServer
        {
            QueryResult = new List<Dictionary<string, object?>> { ComputerRow() }
        };
        var service = new ComputerRegistryService(helper);

        var record = await service.LookupComputerAsync("johnspc", "d8-43-ae-47-d0-d6");

        Assert.IsNotNull(record);
        Assert.AreEqual(1, record.Id);
        Assert.AreEqual("johnspc", record.ComputerName);
        Assert.AreEqual("John's Computer", record.DisplayName);
        Assert.AreEqual("press room", record.Description);
        Assert.AreEqual("d8-43-ae-47-d0-d6", record.MacAddressNormalized);
        Assert.IsTrue(record.IsRegistered);
        Assert.AreEqual(1, helper.QueryCallCount);
    }

    [TestMethod]
    public async Task LookupComputerAsync_WhenNoRows_ReturnsNullAsync()
    {
        var helper = new FakeMySqlHelperServer { QueryResult = new List<Dictionary<string, object?>>() };
        var service = new ComputerRegistryService(helper);

        var record = await service.LookupComputerAsync("johnspc", "d8-43-ae-47-d0-d6");

        Assert.IsNull(record);
    }

    [TestMethod]
    public async Task LookupComputerByMacAsync_ReturnsLatestRecordAsync()
    {
        var helper = new FakeMySqlHelperServer
        {
            QueryResult = new List<Dictionary<string, object?>> { ComputerRow(computerName: "old-host") }
        };
        var service = new ComputerRegistryService(helper);

        var record = await service.LookupComputerByMacAsync("d8-43-ae-47-d0-d6");

        Assert.IsNotNull(record);
        Assert.AreEqual("old-host", record.ComputerName);
    }

    [TestMethod]
    public async Task UpsertComputerAsync_InsertsThenLooksUpAsync()
    {
        var helper = new FakeMySqlHelperServer
        {
            NonQueryResult = 1,
            QueryResult = new List<Dictionary<string, object?>> { ComputerRow(displayName: "John's Computer") }
        };
        var service = new ComputerRegistryService(helper);

        var record = await service.UpsertComputerAsync("johnspc", "johnspc", "d8-43-ae-47-d0-d6", "John's Computer", null);

        Assert.IsNotNull(record);
        Assert.AreEqual("John's Computer", record.DisplayName);
        Assert.AreEqual(1, helper.NonQueryCallCount);
        Assert.AreEqual(1, helper.QueryCallCount);
    }

    [TestMethod]
    public async Task UpsertComputerAsync_WhenLookupReturnsEmpty_ThrowsAsync()
    {
        var helper = new FakeMySqlHelperServer
        {
            NonQueryResult = 1,
            QueryResult = new List<Dictionary<string, object?>>()
        };
        var service = new ComputerRegistryService(helper);

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => service.UpsertComputerAsync("johnspc", "johnspc", "d8-43-ae-47-d0-d6", "John's Computer", null));
    }

    [TestMethod]
    public async Task UpdateComputerByMacAsync_UpdatesThenLooksUpAsync()
    {
        var helper = new FakeMySqlHelperServer
        {
            NonQueryResult = 1,
            QueryResult = new List<Dictionary<string, object?>> { ComputerRow(computerName: "new-host", displayName: "New Name") }
        };
        var service = new ComputerRegistryService(helper);

        var record = await service.UpdateComputerByMacAsync("d8-43-ae-47-d0-d6", "new-host", "new-host", "New Name", null);

        Assert.IsNotNull(record);
        Assert.AreEqual("new-host", record.ComputerName);
        Assert.AreEqual("New Name", record.DisplayName);
        Assert.AreEqual(1, helper.NonQueryCallCount);
    }

    private static Dictionary<string, object?> ComputerRow(
        long id = 1,
        string computerName = "johnspc",
        string displayName = "John's Computer",
        string description = "press room",
        string mac = "d8-43-ae-47-d0-d6",
        long isRegistered = 1)
    {
        return new Dictionary<string, object?>
        {
            ["id"] = id,
            ["computer_name"] = computerName,
            ["display_name"] = displayName,
            ["description"] = description,
            ["mac_address_normalized"] = mac,
            ["is_registered"] = isRegistered
        };
    }

    private sealed class FakeMySqlHelperServer : IMySqlHelperServer
    {
        public List<Dictionary<string, object?>> QueryResult { get; set; } = new();

        public int NonQueryResult { get; set; }

        public int QueryCallCount { get; private set; }

        public int NonQueryCallCount { get; private set; }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(string sql, IReadOnlyDictionary<string, object?> parameters, MySqlDatabaseTarget databaseTarget, CancellationToken cancellationToken = default)
        {
            QueryCallCount++;
            return Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(QueryResult);
        }

        public Task<int> ExecuteSqlNonQueryAsync(string sql, IReadOnlyDictionary<string, object?> parameters, MySqlDatabaseTarget databaseTarget, CancellationToken cancellationToken = default)
        {
            NonQueryCallCount++;
            return Task.FromResult(NonQueryResult);
        }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(string storedProcedureName, IReadOnlyDictionary<string, object?> parameters, MySqlDatabaseTarget databaseTarget, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(new List<Dictionary<string, object?>>());
        }

        public Task<int> ExecuteStoredProcedureNonQueryAsync(string storedProcedureName, IReadOnlyDictionary<string, object?> parameters, MySqlDatabaseTarget databaseTarget, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }
    }
}
