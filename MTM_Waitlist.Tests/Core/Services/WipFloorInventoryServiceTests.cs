using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class WipFloorInventoryServiceTests
{
    [TestMethod]
    public async Task GetFloorSnapshotAsync_ReturnsMappedSnapshot_WhenScriptAndRowAvailable()
    {
        var stub = new StubMySqlHelperServer(new[]
        {
            FloorRow(finishedGoods: 10m, outsideService: 74m, nonConforming: 0m, wip: 33m),
        });

        var service = new WipFloorInventoryService(stub);
        var snapshot = await service.GetFloorSnapshotAsync("A22-77724-100");

        Assert.IsNotNull(snapshot);
        Assert.AreEqual(10m, snapshot!.FinishedGoodsFloorQuantity);
        Assert.AreEqual(74m, snapshot.OutsideServiceFloorQuantity);
        Assert.AreEqual(0m, snapshot.NonConformingFloorQuantity);
        Assert.AreEqual(33m, snapshot.WipFloorQuantity);
        Assert.IsTrue(snapshot.HasAnyFloorQuantity);

        // The queue script must be executed against the MTM WIP Application database target.
        Assert.AreEqual(MySqlDatabaseTarget.MtmWipApplication, stub.LastDatabaseTarget);
        Assert.AreEqual(1, stub.QueryCallCount);
    }

    [TestMethod]
    public async Task GetFloorSnapshotAsync_TrimsAndNormalizesPartNumber()
    {
        var stub = new StubMySqlHelperServer(new[] { FloorRow(wip: 5m) });
        var service = new WipFloorInventoryService(stub);

        var snapshot = await service.GetFloorSnapshotAsync("  380397.001  ");

        Assert.IsNotNull(snapshot);
        Assert.AreEqual("380397.001", stub.LastPartNumber);
    }

    [TestMethod]
    public async Task GetFloorSnapshotAsync_NoRows_ReturnsNull()
    {
        var stub = new StubMySqlHelperServer(Array.Empty<Dictionary<string, object?>>());
        var service = new WipFloorInventoryService(stub);

        var snapshot = await service.GetFloorSnapshotAsync("ZZZ-NOT-A-PART");

        Assert.IsNull(snapshot);
    }

    [TestMethod]
    public async Task GetFloorSnapshotAsync_BlankPart_ReturnsNullWithoutCallingBackend()
    {
        var stub = new StubMySqlHelperServer(new[] { FloorRow(wip: 5m) });
        var service = new WipFloorInventoryService(stub);

        var snapshot = await service.GetFloorSnapshotAsync("   ");

        Assert.IsNull(snapshot);
        Assert.AreEqual(0, stub.QueryCallCount);
    }

    [TestMethod]
    public async Task GetFloorSnapshotAsync_AllZeroBuckets_SnapshotHasNoFloorQuantity()
    {
        var stub = new StubMySqlHelperServer(new[] { FloorRow(finishedGoods: 0m, outsideService: 0m, nonConforming: 0m, wip: 0m) });
        var service = new WipFloorInventoryService(stub);

        var snapshot = await service.GetFloorSnapshotAsync("A18-71252-001");

        Assert.IsNotNull(snapshot);
        Assert.IsFalse(snapshot!.HasAnyFloorQuantity);
    }

    private static Dictionary<string, object?> FloorRow(
        decimal finishedGoods = 0m,
        decimal outsideService = 0m,
        decimal nonConforming = 0m,
        decimal wip = 0m) => new()
    {
        ["FinishedGoodsFloorQuantity"] = finishedGoods,
        ["OutsideServiceFloorQuantity"] = outsideService,
        ["NonConformingFloorQuantity"] = nonConforming,
        ["WipFloorQuantity"] = wip,
    };

    private sealed class StubMySqlHelperServer : IMySqlHelperServer
    {
        private readonly IReadOnlyList<Dictionary<string, object?>> _rows;

        public StubMySqlHelperServer(IReadOnlyList<Dictionary<string, object?>> rows) => _rows = rows;

        public int QueryCallCount { get; private set; }

        public MySqlDatabaseTarget? LastDatabaseTarget { get; private set; }

        public string? LastPartNumber { get; private set; }

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
            => Task.FromResult(1);

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            QueryCallCount++;
            LastDatabaseTarget = databaseTarget;
            LastPartNumber = parameters.TryGetValue("PartNumber", out var part) ? Convert.ToString(part) : null;
            return Task.FromResult(_rows);
        }

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(1);
    }
}
