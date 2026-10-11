using System.Text.Json;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.Services;

namespace MTM_Waitlist.Tests.Module_Setup.Services;

[TestClass]
public sealed class ActiveJobItemResolverServiceTests
{
    [TestMethod]
    public async Task Service_ActiveJobItemResolverServiceTests_ResolveAsync_BlankWorkCenterReturnsNullNoQuery()
    {
        var helper = new StubMySqlHelperServer(Array.Empty<Dictionary<string, object?>>());
        var service = new ActiveJobItemResolverService(helper);

        var result = await service.ResolveAsync("   ");

        Assert.IsNull(result);
        Assert.AreEqual(0, helper.QueryCallCount);
    }

    [TestMethod]
    public async Task Service_ActiveJobItemResolverServiceTests_ResolveAsync_NoActiveJobForWorkCenterReturnsNull()
    {
        var helper = new StubMySqlHelperServer(Array.Empty<Dictionary<string, object?>>());
        var service = new ActiveJobItemResolverService(helper);

        var result = await service.ResolveAsync("PRESS-99");

        Assert.IsNull(result);
        Assert.AreEqual(1, helper.QueryCallCount);
    }

    [TestMethod]
    public async Task Service_ActiveJobItemResolverServiceTests_ResolveAsync_JobItemsReturnsCoilFlatstockDieComponent()
    {
        var parts = new List<SetupSubordinatePart>
        {
            MakePart("Coil", "MMC0001000", "Rack A1", 5000m),
            MakePart("Flatstock", "MMF0001154", "Rack B2", 0m),
            MakePart("Die", "FGT-0653", "V-A1-01", 0m),
            MakePart("Component", "880055", "Bin 7", 3m),
        };
        var helper = new StubMySqlHelperServer(new[]
        {
            ActiveJobRow("PRESS-01", "WO-074011", "22-77401-001", "30", parts, dunnage: null),
        });
        var service = new ActiveJobItemResolverService(helper);

        var snapshot = await service.ResolveAsync("PRESS-01");

        Assert.IsNotNull(snapshot);
        Assert.AreEqual("WO-074011", snapshot!.WorkOrder);
        Assert.AreEqual(4, snapshot.SubordinateParts.Count);
        Assert.AreEqual(1, snapshot.Coils.Count);
        Assert.AreEqual(1, snapshot.Flatstock.Count);
        Assert.AreEqual(1, snapshot.Dies.Count);
        Assert.AreEqual(1, snapshot.Components.Count);
        Assert.AreEqual("MMC0001000", snapshot.Coils[0].PartNumber);
    }

    [TestMethod]
    public async Task Service_ActiveJobItemResolverServiceTests_ResolveAsync_DieLocationSurfacesHomeLocation()
    {
        var parts = new List<SetupSubordinatePart> { MakePart("Die", "FGT-0653", "V-A1-01", 0m) };
        var helper = new StubMySqlHelperServer(new[]
        {
            ActiveJobRow("PRESS-01", "WO-074011", "22-77401-001", "30", parts, dunnage: null),
        });
        var service = new ActiveJobItemResolverService(helper);

        var snapshot = await service.ResolveAsync("PRESS-01");

        Assert.IsNotNull(snapshot);
        Assert.AreEqual("V-A1-01", snapshot!.DieLocation);
        Assert.AreEqual("FGT-0653", snapshot.PrimaryDie?.PartNumber);
    }

    [TestMethod]
    public async Task Service_ActiveJobItemResolverServiceTests_ResolveAsync_DunnagePartsAreReturned()
    {
        var dunnage = new List<SetupDunnagePart>
        {
            new() { Id = "dn-1", TypeId = "stl-rack", PartNumber = "DN-STL-4", DisplayName = "Steel Rack" },
        };
        var helper = new StubMySqlHelperServer(new[]
        {
            ActiveJobRow("PRESS-01", "WO-074011", "22-77401-001", "30", parts: null, dunnage),
        });
        var service = new ActiveJobItemResolverService(helper);

        var snapshot = await service.ResolveAsync("PRESS-01");

        Assert.IsNotNull(snapshot);
        Assert.AreEqual(1, snapshot!.DunnageParts.Count);
        Assert.AreEqual("DN-STL-4", snapshot.DunnageParts[0].PartNumber);
    }

    [TestMethod]
    public async Task Service_ActiveJobItemResolverServiceTests_ResolveAsync_SequenceReturnsSequenceNumber()
    {
        var helper = new StubMySqlHelperServer(new[]
        {
            ActiveJobRow("PRESS-01", "WO-074011", "22-77401-001", "42", parts: null, dunnage: null),
        });
        var service = new ActiveJobItemResolverService(helper);

        var snapshot = await service.ResolveAsync("PRESS-01");

        Assert.IsNotNull(snapshot);
        Assert.AreEqual("42", snapshot!.SequenceNumber);
    }

    [TestMethod]
    public async Task Service_ActiveJobItemResolverServiceTests_ResolveAsync_PrefixCanonicalizationOverridesMisTaggedCategory()
    {
        // Simulate the SetupDataCatalog quirk: an MMF part persisted with Category "Component".
        var parts = new List<SetupSubordinatePart> { MakePart("Component", "MMF0001154", "Rack B2", 0m) };
        var helper = new StubMySqlHelperServer(new[]
        {
            ActiveJobRow("PRESS-01", "WO-074011", "22-77401-001", "30", parts, dunnage: null),
        });
        var service = new ActiveJobItemResolverService(helper);

        var snapshot = await service.ResolveAsync("PRESS-01");

        Assert.IsNotNull(snapshot);
        Assert.AreEqual(0, snapshot!.Components.Count);
        Assert.AreEqual(1, snapshot.Flatstock.Count);
        Assert.AreEqual("MMF0001154", snapshot.Flatstock[0].PartNumber);
    }

    [TestMethod]
    public async Task Service_ActiveJobItemResolverServiceTests_ResolveAsync_BlankOrMalformedJsonReturnsEmptyLists()
    {
        var helper = new StubMySqlHelperServer(new[]
        {
            ActiveJobRow("PRESS-01", "WO-074011", "22-77401-001", "30", parts: null, dunnage: null, partsJson: "not json", dunnageJson: "not json"),
        });
        var service = new ActiveJobItemResolverService(helper);

        var snapshot = await service.ResolveAsync("PRESS-01");

        Assert.IsNotNull(snapshot);
        Assert.AreEqual(0, snapshot!.SubordinateParts.Count);
        Assert.AreEqual(0, snapshot.DunnageParts.Count);
    }

    private static SetupSubordinatePart MakePart(string category, string partNumber, string location, decimal onHand)
        => new()
        {
            Category = category,
            PartNumber = partNumber,
            Description = $"Part {partNumber}",
            Location = location,
            OnHandQuantity = onHand,
        };

    private static Dictionary<string, object?> ActiveJobRow(
        string workCenter,
        string workOrder,
        string partNumber,
        string sequence,
        List<SetupSubordinatePart>? parts,
        List<SetupDunnagePart>? dunnage,
        string? partsJson = null,
        string? dunnageJson = null)
        => new()
        {
            ["work_center"] = workCenter,
            ["work_order"] = workOrder,
            ["part_number"] = partNumber,
            ["sequence_number"] = sequence,
            ["subordinate_parts_json"] = partsJson ?? (parts is null ? null : JsonSerializer.Serialize(parts)),
            ["selected_dunnage_parts_json"] = dunnageJson ?? (dunnage is null ? null : JsonSerializer.Serialize(dunnage)),
        };

    private sealed class StubMySqlHelperServer : IMySqlHelperServer
    {
        private readonly IReadOnlyList<Dictionary<string, object?>> _rows;

        public StubMySqlHelperServer(IReadOnlyList<Dictionary<string, object?>> rows) => _rows = rows;

        public int QueryCallCount { get; private set; }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            QueryCallCount++;
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
            => Task.FromResult(0);
    }
}
