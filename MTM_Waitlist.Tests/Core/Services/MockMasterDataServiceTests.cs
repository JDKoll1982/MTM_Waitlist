using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class MockMasterDataServiceTests
{
    [TestMethod]
    public async Task GetMasterTablesAsync_MapsRegistryRows_OrderedByRank()
    {
        // The service maps rows as returned by the SQL query (which orders by sort_rank), so the
        // stub rows are provided already in that DB order.
        var rows = new[]
        {
            RegistryRow(tableName: "mock_locations", ui: "Mock Locations", desc: "Locations", rank: 10),
            RegistryRow(tableName: "mock_parts", ui: "Mock Parts", desc: "Parts list", rank: 20),
        };
        var service = new MockMasterDataService(new StubMySqlHelperServer(rows));

        var result = await service.GetMasterTablesAsync();

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("mock_locations", result[0].TableName);
        Assert.AreEqual("Mock Parts", result[1].UiDisplayName);
        Assert.AreEqual("Parts list", result[1].DescriptionText);
    }

    [TestMethod]
    public async Task ReadTableRowsAsync_AllowedTable_ReturnsRows_AsDictionaries()
    {
        var row = new Dictionary<string, object?> { ["part_number"] = "MMC0001000", ["part_category"] = "Coil" };
        var service = new MockMasterDataService(new StubMySqlHelperServer(new[] { row }));

        var result = await service.ReadTableRowsAsync("mock_parts");

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("MMC0001000", result[0]["part_number"]);
    }

    [TestMethod]
    public async Task ReadTableRowsAsync_DisallowedOrBlankTable_ReturnsEmpty()
    {
        var service = new MockMasterDataService(new StubMySqlHelperServer(Array.Empty<Dictionary<string, object?>>()));

        Assert.AreEqual(0, (await service.ReadTableRowsAsync("mock_parts; DROP TABLE x")).Count);
        Assert.AreEqual(0, (await service.ReadTableRowsAsync("   ")).Count);
    }

    [TestMethod]
    public async Task GetTableColumnsAsync_MapsColumnMetadata()
    {
        var columns = new[]
        {
            ColumnRow("part_number", "varchar", "NO", audit: 0),
            ColumnRow("is_active", "tinyint", "NO", audit: 0),
            ColumnRow("id", "bigint", "NO", audit: 1),
            ColumnRow("public_id", "char", "NO", audit: 1),
        };
        var service = new MockMasterDataService(new StubMySqlHelperServer(columns));

        var result = await service.GetTableColumnsAsync("mock_parts");

        Assert.AreEqual(4, result.Count);
        Assert.AreEqual("part_number", result[0].ColumnName);
        Assert.AreEqual("varchar", result[0].DataType);
        Assert.IsFalse(result[0].IsAudit);
        Assert.IsTrue(result[2].IsAudit);
    }

    [TestMethod]
    public async Task GetTableColumnsAsync_DisallowedTable_ReturnsEmpty()
    {
        var service = new MockMasterDataService(new StubMySqlHelperServer(Array.Empty<Dictionary<string, object?>>()));

        Assert.AreEqual(0, (await service.GetTableColumnsAsync("waitlist_request_types")).Count);
        Assert.AreEqual(0, (await service.GetTableColumnsAsync("  ")).Count);
    }

    [TestMethod]
    public async Task AddTableRowAsync_DispatchesToInsertSp_WithEditableColumns()
    {
        var stub = new StubMySqlHelperServer(Array.Empty<Dictionary<string, object?>>());
        var service = new MockMasterDataService(stub);
        var values = new Dictionary<string, object?>
        {
            ["part_number"] = "MMC0001000",
            ["part_category"] = "Coil",
            ["part_description"] = "Primary coil",
            ["unit_of_measure"] = "lb",
            ["is_active"] = (byte)1, // not accepted by the insert SP -> ignored
        };

        var ok = await service.AddTableRowAsync("mock_parts", values);

        Assert.IsTrue(ok);
        Assert.AreEqual("sp_mock_parts_insert", stub.LastNonQueryProcedure);
        Assert.IsNotNull(stub.LastNonQueryParameters);
        Assert.AreEqual("MMC0001000", stub.LastNonQueryParameters["p_part_number"]);
        Assert.IsFalse(stub.LastNonQueryParameters.ContainsKey("p_is_active"));
    }

    [TestMethod]
    public async Task UpdateTableRowAsync_DispatchesToUpdateSp_WithId()
    {
        var stub = new StubMySqlHelperServer(Array.Empty<Dictionary<string, object?>>());
        var service = new MockMasterDataService(stub);
        var values = new Dictionary<string, object?>
        {
            ["part_number"] = "MMC0001000",
            ["part_category"] = "Coil",
            ["part_description"] = "Primary coil v2",
            ["unit_of_measure"] = "lb",
        };

        var ok = await service.UpdateTableRowAsync("mock_parts", 7L, values);

        Assert.IsTrue(ok);
        Assert.AreEqual("sp_mock_parts_update", stub.LastNonQueryProcedure);
        Assert.AreEqual(7L, stub.LastNonQueryParameters!["p_id"]);
        Assert.AreEqual("Primary coil v2", stub.LastNonQueryParameters["p_part_description"]);
    }

    [TestMethod]
    public async Task DeleteTableRowAsync_DispatchesToDeleteSp()
    {
        var stub = new StubMySqlHelperServer(Array.Empty<Dictionary<string, object?>>());
        var service = new MockMasterDataService(stub);

        var ok = await service.DeleteTableRowAsync("mock_requesters", 3L);

        Assert.IsTrue(ok);
        Assert.AreEqual("sp_mock_requesters_delete", stub.LastNonQueryProcedure);
        Assert.AreEqual(3L, stub.LastNonQueryParameters!["p_id"]);
    }

    [TestMethod]
    public async Task WriteOperations_DisallowedTable_ReturnsFalse_WithoutCalling()
    {
        var stub = new StubMySqlHelperServer(Array.Empty<Dictionary<string, object?>>());
        var service = new MockMasterDataService(stub);

        Assert.IsFalse(await service.AddTableRowAsync("waitlist_request_types", new Dictionary<string, object?> { ["x"] = 1 }));
        Assert.IsFalse(await service.UpdateTableRowAsync("  ", 1L, new Dictionary<string, object?>()));
        Assert.IsFalse(await service.DeleteTableRowAsync("mock_parts; DROP", 1L));
        Assert.IsNull(stub.LastNonQueryProcedure);
    }

    private static Dictionary<string, object?> ColumnRow(string name, string type, string nullable, int audit) => new()
    {
        ["column_name"] = name,
        ["data_type"] = type,
        ["is_nullable"] = nullable,
        ["is_audit"] = (byte)audit,
    };

    private static Dictionary<string, object?> RegistryRow(string tableName, string ui, string desc, int rank) => new()
    {
        ["id"] = 1L,
        ["public_id"] = Guid.NewGuid().ToString(),
        ["table_name"] = tableName,
        ["ui_display_name"] = ui,
        ["description_text"] = desc,
        ["is_active"] = (byte)1,
        ["sort_rank"] = rank,
    };

    private sealed class StubMySqlHelperServer : IMySqlHelperServer
    {
        private readonly IReadOnlyList<Dictionary<string, object?>> _rows;

        public StubMySqlHelperServer(IReadOnlyList<Dictionary<string, object?>> rows) => _rows = rows;

        public MySqlDatabaseTarget? LastDatabaseTarget { get; private set; }

        public string? LastNonQueryProcedure { get; private set; }

        public IReadOnlyDictionary<string, object?>? LastNonQueryParameters { get; private set; }

        public int NonQueryResult { get; set; } = 1;

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_rows);

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            LastNonQueryProcedure = storedProcedureName;
            LastNonQueryParameters = new Dictionary<string, object?>(parameters, StringComparer.OrdinalIgnoreCase);
            LastDatabaseTarget = databaseTarget;
            return Task.FromResult(NonQueryResult);
        }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            LastDatabaseTarget = databaseTarget;
            return Task.FromResult(_rows);
        }

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(0);
    }
}
