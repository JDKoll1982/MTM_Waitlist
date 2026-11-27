using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class WipFloorInventoryServiceTests
{
    [TestMethod]
    public async Task Service_WipFloorInventoryServiceTests_GetFloorSnapshotAsync_ReturnsMappedSnapshotWhenScriptAndRowAvailable()
    {
        var stub = new StubReviewedScriptReader(new[]
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

        // The reviewed route has one store, so the target is structural; the script name is what proves the
        // floor-quantity script is the one that ran.
        Assert.AreEqual("GetWipFloorQuantities", stub.LastScriptName);
        Assert.AreEqual(1, stub.QueryCallCount);
    }

    [TestMethod]
    public async Task Service_WipFloorInventoryServiceTests_GetFloorSnapshotAsync_TrimsAndNormalizesPartNumber()
    {
        var stub = new StubReviewedScriptReader(new[] { FloorRow(wip: 5m) });
        var service = new WipFloorInventoryService(stub);

        var snapshot = await service.GetFloorSnapshotAsync("  380397.001  ");

        Assert.IsNotNull(snapshot);
        Assert.AreEqual("380397.001", stub.LastPartNumber);
    }

    [TestMethod]
    public async Task Service_WipFloorInventoryServiceTests_GetFloorSnapshotAsync_NoRowsReturnsNull()
    {
        var stub = new StubReviewedScriptReader(Array.Empty<Dictionary<string, object?>>());
        var service = new WipFloorInventoryService(stub);

        var snapshot = await service.GetFloorSnapshotAsync("ZZZ-NOT-A-PART");

        Assert.IsNull(snapshot);
    }

    [TestMethod]
    public async Task Service_WipFloorInventoryServiceTests_GetFloorSnapshotAsync_BlankPartReturnsNullWithoutCallingBackend()
    {
        var stub = new StubReviewedScriptReader(new[] { FloorRow(wip: 5m) });
        var service = new WipFloorInventoryService(stub);

        var snapshot = await service.GetFloorSnapshotAsync("   ");

        Assert.IsNull(snapshot);
        Assert.AreEqual(0, stub.QueryCallCount);
    }

    [TestMethod]
    public async Task Service_WipFloorInventoryServiceTests_GetFloorSnapshotAsync_AllZeroBucketsSnapshotHasNoFloorQuantity()
    {
        var stub = new StubReviewedScriptReader(new[] { FloorRow(finishedGoods: 0m, outsideService: 0m, nonConforming: 0m, wip: 0m) });
        var service = new WipFloorInventoryService(stub);

        var snapshot = await service.GetFloorSnapshotAsync("A18-71252-001");

        Assert.IsNotNull(snapshot);
        Assert.IsFalse(snapshot!.HasAnyFloorQuantity);
    }

    /// <summary>
    /// The WIP inventory's distinct part numbers come from their own checked-in queue script, read through the
    /// same seam and against the same store the floor snapshot is read from. A part the WIP floor can name is a
    /// part this application can picture (FR-001).
    /// </summary>
    [TestMethod]
    public async Task Service_WipFloorInventoryServiceTests_GetInventoryPartNumbersAsync_ReturnsTheScriptsRowsThroughTheExistingSeam()
    {
        var stub = new StubReviewedScriptReader(new[]
        {
            new Dictionary<string, object?> { ["PartNumber"] = "MMC0001000" },
            new Dictionary<string, object?> { ["PartNumber"] = "  A22-77724-100  " },
        });

        var service = new WipFloorInventoryService(stub);
        var partNumbers = await service.GetInventoryPartNumbersAsync();

        CollectionAssert.AreEqual(
            new[] { "MMC0001000", "A22-77724-100" },
            partNumbers.ToArray(),
            "The part numbers come back trimmed and in the script's own order.");

        Assert.AreEqual("GetWipInventoryPartNumbers", stub.LastScriptName);
        var scriptText = await WaitlistWipMySqlScriptStore.LoadAsync("GetWipInventoryPartNumbers");
        StringAssert.Contains(scriptText, "AS PartNumber", "The read must execute the part-number script, not the floor-quantity one.");
        StringAssert.Contains(scriptText, "inv_inventory", "The read must execute the part-number script, not the floor-quantity one.");
        StringAssert.Contains(scriptText, "Dunnage", "Dunnage rows are packaging stock and are not parts the application pictures.");
        Assert.AreEqual(0, stub.LastParameterCount, "The part-number read is not parameterised: it lists the floor.");
    }

    /// <summary>An empty floor is an empty list, not a failure and not a fabricated part.</summary>
    [TestMethod]
    public async Task Service_WipFloorInventoryServiceTests_GetInventoryPartNumbersAsync_NoRowsReturnsAnEmptyList()
    {
        var stub = new StubReviewedScriptReader(Array.Empty<Dictionary<string, object?>>());
        var service = new WipFloorInventoryService(stub);

        Assert.AreEqual(0, (await service.GetInventoryPartNumbersAsync()).Count);
    }

    /// <summary>
    /// A row whose part number is blank names no part, so it is left out rather than offered as a part with no
    /// key. A repeated number is one part, not two.
    /// </summary>
    [TestMethod]
    public async Task Service_WipFloorInventoryServiceTests_GetInventoryPartNumbersAsync_LeavesOutBlankPartNumbersAndRepeats()
    {
        var stub = new StubReviewedScriptReader(new[]
        {
            new Dictionary<string, object?> { ["PartNumber"] = "MMC0001000" },
            new Dictionary<string, object?> { ["PartNumber"] = "   " },
            new Dictionary<string, object?> { ["PartNumber"] = "mmc0001000" },
        });

        var service = new WipFloorInventoryService(stub);

        CollectionAssert.AreEqual(new[] { "MMC0001000" }, (await service.GetInventoryPartNumbersAsync()).ToArray());
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

    private sealed class StubReviewedScriptReader : IReviewedQueueScriptReader
    {
        private readonly IReadOnlyList<Dictionary<string, object?>> _rows;

        public StubReviewedScriptReader(IReadOnlyList<Dictionary<string, object?>> rows) => _rows = rows;

        public int QueryCallCount { get; private set; }

        public string? LastScriptName { get; private set; }

        public string? LastPartNumber { get; private set; }

        public int LastParameterCount { get; private set; }

        public Task<IReadOnlyList<Dictionary<string, object?>>?> ExecuteReviewedScriptQueryAsync(
            string reviewedScriptName,
            IReadOnlyDictionary<string, object?> parameters,
            CancellationToken cancellationToken = default)
        {
            QueryCallCount++;
            LastScriptName = reviewedScriptName;
            LastParameterCount = parameters.Count;
            LastPartNumber = parameters.TryGetValue("PartNumber", out var part) ? Convert.ToString(part) : null;
            return Task.FromResult<IReadOnlyList<Dictionary<string, object?>>?>(_rows);
        }
    }
}
