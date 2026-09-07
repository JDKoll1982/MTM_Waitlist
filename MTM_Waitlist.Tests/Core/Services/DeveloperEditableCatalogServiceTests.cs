using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class DeveloperEditableCatalogServiceTests
{
    [TestMethod]
    public async Task GetTablesAsync_ReturnsMockTables_ThenRealRequestTables()
    {
        var service = new DeveloperEditableCatalogService(new StubMockMasterDataService(new[]
        {
            new MockMasterTableDefinition { TableName = "mock_parts", UiDisplayName = "Mock Parts", DescriptionText = "Parts", SortRank = 1 },
            new MockMasterTableDefinition { TableName = "mock_locations", UiDisplayName = "Mock Locations", DescriptionText = "Locations", SortRank = 2 },
        }));

        var tables = await service.GetTablesAsync();

        Assert.AreEqual(4, tables.Count);
        Assert.AreEqual("mock_parts", tables[0].TableKey);
        Assert.AreEqual(DeveloperTableSourceKind.Mock, tables[0].SourceKind);
        Assert.AreEqual("mock_locations", tables[1].TableKey);
        // Real request tables come last, flagged RealCatalog.
        Assert.AreEqual("waitlist_request_types", tables[2].TableKey);
        Assert.AreEqual(DeveloperTableSourceKind.RealCatalog, tables[2].SourceKind);
        Assert.AreEqual("waitlist_request_subtypes", tables[3].TableKey);
        Assert.AreEqual(DeveloperTableSourceKind.RealCatalog, tables[3].SourceKind);
    }

    [TestMethod]
    public async Task GetTablesAsync_EmptyMockRegistry_StillReturnsRealRequestTables()
    {
        var service = new DeveloperEditableCatalogService(new StubMockMasterDataService(Array.Empty<MockMasterTableDefinition>()));

        var tables = await service.GetTablesAsync();

        Assert.AreEqual(2, tables.Count);
        Assert.AreEqual(DeveloperTableSourceKind.RealCatalog, tables[0].SourceKind);
        Assert.AreEqual(DeveloperTableSourceKind.RealCatalog, tables[1].SourceKind);
    }

    [TestMethod]
    public async Task GetTablesAsync_BlankMockEntries_AreSkipped()
    {
        var service = new DeveloperEditableCatalogService(new StubMockMasterDataService(new[]
        {
            new MockMasterTableDefinition { TableName = "", UiDisplayName = "", SortRank = 1 },
            new MockMasterTableDefinition { TableName = "mock_parts", UiDisplayName = "", SortRank = 2 },
        }));

        var tables = await service.GetTablesAsync();

        // Only the valid mock table + the two real tables.
        Assert.AreEqual(3, tables.Count);
        Assert.AreEqual("mock_parts", tables[0].TableKey);
    }

    private sealed class StubMockMasterDataService : IMockMasterDataService
    {
        private readonly IReadOnlyList<MockMasterTableDefinition> _tables;

        public StubMockMasterDataService(IReadOnlyList<MockMasterTableDefinition> tables) => _tables = tables;

        public Task<IReadOnlyList<MockMasterTableDefinition>> GetMasterTablesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_tables);

        public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReadTableRowsAsync(string tableName, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>(Array.Empty<IReadOnlyDictionary<string, object?>>());

        public Task<IReadOnlyList<MockMasterColumnDefinition>> GetTableColumnsAsync(string tableName, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MockMasterColumnDefinition>>(Array.Empty<MockMasterColumnDefinition>());

        public Task<bool> AddTableRowAsync(string tableName, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<bool> UpdateTableRowAsync(string tableName, long id, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<bool> DeleteTableRowAsync(string tableName, long id, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }
}
