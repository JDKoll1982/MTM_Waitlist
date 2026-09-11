using System.Text.Json;

using Microsoft.Extensions.Logging.Abstractions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Service.Services;
using MTM_Waitlist.Mock.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// Verifies the payload contract the service must satisfy for every refresh: the keys are exactly the shape's
/// inputs then its outputs in catalog order, flags reach the procedure as 1/0, drift is reported as a schema
/// mismatch, and unreachability is distinguishable from a failed read (FR-008, FR-020).
/// </summary>
[TestClass]
public sealed class VisualShapePayloadSourceTests
{
    [TestMethod]
    public async Task BuildRefreshPayloadAsync_ReadsTheShapesPopulationScript_NotItsPerKeyScript()
    {
        var executor = new StubExecutor(Row(("NormalizedWorkOrder", "WO-1"), ("PartNumber", "P-1"), ("Description", "Ring"), ("WorkCenter", "WC-1")));
        var source = new VisualShapePayloadSource(executor, NullLogger<VisualShapePayloadSource>.Instance);
        var shape = RequireShape("work_order_lookup");

        await source.BuildRefreshPayloadAsync(shape);

        Assert.AreEqual(shape.PopulationScriptRelativePath, executor.LastScriptPath);
        Assert.AreNotEqual(shape.SourceScriptRelativePath, executor.LastScriptPath);
        Assert.AreEqual(0, executor.LastParameters!.Count, "The set-based population read takes no parameters.");
    }

    [TestMethod]
    public async Task BuildRefreshPayloadAsync_KeysThePayloadByInputsThenOutputs_InCatalogOrder()
    {
        // Deliberately supply the columns in a different order than the catalog declares.
        var executor = new StubExecutor(Row(
            ("WorkCenter", "WC-1"),
            ("NormalizedWorkOrder", "WO-1"),
            ("Description", "Ring"),
            ("PartNumber", "P-1")));

        var source = new VisualShapePayloadSource(executor, NullLogger<VisualShapePayloadSource>.Instance);
        var payload = await source.BuildRefreshPayloadAsync(RequireShape("work_order_lookup"));

        using var document = JsonDocument.Parse(payload);
        var keys = document.RootElement[0].EnumerateObject().Select(property => property.Name).ToList();

        CollectionAssert.AreEqual(
            new[] { "NormalizedWorkOrder", "PartNumber", "Description", "WorkCenter" },
            keys,
            "The payload key order must follow the catalog, because that is the order the procedure extracts.");
    }

    [TestMethod]
    public async Task BuildRefreshPayloadAsync_EncodesABitColumnAsOneOrZero()
    {
        // The procedures read a flag with CAST(JSON_UNQUOTE(...) AS SIGNED), where a JSON boolean would
        // become 0 — so a true flag must be serialized as the number 1.
        var executor = new StubExecutor(Row(
            ("WorkOrder", "WO-1"),
            ("PartNumber", "P-1"),
            ("WorkOrderStatus", "R"),
            ("OpenWorkOrderQuantity", 12.5m),
            ("FinishedGoodsQuantity", 0m),
            ("HasOutsideVendorOperation", true)));

        var source = new VisualShapePayloadSource(executor, NullLogger<VisualShapePayloadSource>.Instance);
        var payload = await source.BuildRefreshPayloadAsync(RequireShape("disposition_input"));

        using var document = JsonDocument.Parse(payload);
        var row = document.RootElement[0];

        Assert.AreEqual(JsonValueKind.Number, row.GetProperty("HasOutsideVendorOperation").ValueKind);
        Assert.AreEqual(1, row.GetProperty("HasOutsideVendorOperation").GetInt32());
        Assert.AreEqual(12.5m, row.GetProperty("OpenWorkOrderQuantity").GetDecimal());
        Assert.AreEqual("R", row.GetProperty("WorkOrderStatus").GetString());
    }

    [TestMethod]
    public async Task BuildRefreshPayloadAsync_AFlagThatIsFalse_IsZero()
    {
        var executor = new StubExecutor(Row(
            ("WorkOrder", "WO-1"),
            ("PartNumber", "P-1"),
            ("WorkOrderStatus", "C"),
            ("OpenWorkOrderQuantity", 0m),
            ("FinishedGoodsQuantity", 0m),
            ("HasOutsideVendorOperation", false)));

        var source = new VisualShapePayloadSource(executor, NullLogger<VisualShapePayloadSource>.Instance);
        var payload = await source.BuildRefreshPayloadAsync(RequireShape("disposition_input"));

        using var document = JsonDocument.Parse(payload);
        Assert.AreEqual(0, document.RootElement[0].GetProperty("HasOutsideVendorOperation").GetInt32());
    }

    [TestMethod]
    public async Task BuildRefreshPayloadAsync_WhenAColumnIsMissing_ReportsASchemaMismatch()
    {
        var executor = new StubExecutor(Row(("NormalizedWorkOrder", "WO-1"), ("PartNumber", "P-1"), ("Description", "Ring")));
        var source = new VisualShapePayloadSource(executor, NullLogger<VisualShapePayloadSource>.Instance);

        var exception = await Assert.ThrowsExceptionAsync<VisualSourceSchemaMismatchException>(
            () => source.BuildRefreshPayloadAsync(RequireShape("work_order_lookup")));

        StringAssert.Contains(exception.Message, "WorkCenter");
    }

    [TestMethod]
    public async Task BuildRefreshPayloadAsync_WhenTheSourceAddsAColumn_ReportsASchemaMismatch()
    {
        var executor = new StubExecutor(Row(
            ("NormalizedWorkOrder", "WO-1"),
            ("PartNumber", "P-1"),
            ("Description", "Ring"),
            ("WorkCenter", "WC-1"),
            ("WorkOrderStatus", "R")));

        var source = new VisualShapePayloadSource(executor, NullLogger<VisualShapePayloadSource>.Instance);

        var exception = await Assert.ThrowsExceptionAsync<VisualSourceSchemaMismatchException>(
            () => source.BuildRefreshPayloadAsync(RequireShape("work_order_lookup")));

        StringAssert.Contains(exception.Message, "WorkOrderStatus");
    }

    [TestMethod]
    public async Task BuildRefreshPayloadAsync_WithNoRows_ProducesAnEmptyArray()
    {
        var executor = new StubExecutor();
        var source = new VisualShapePayloadSource(executor, NullLogger<VisualShapePayloadSource>.Instance);

        var payload = await source.BuildRefreshPayloadAsync(RequireShape("work_order_lookup"));

        Assert.AreEqual("[]", payload, "An empty population is a legitimate snapshot, not a failure.");
    }

    [TestMethod]
    public async Task BuildRefreshPayloadAsync_WhenTheSourceIsUnreachable_RaisesUnreachable()
    {
        var executor = new StubExecutor { Unreachable = "connection timed out" };
        var source = new VisualShapePayloadSource(executor, NullLogger<VisualShapePayloadSource>.Instance);

        var exception = await Assert.ThrowsExceptionAsync<VisualSourceUnreachableException>(
            () => source.BuildRefreshPayloadAsync(RequireShape("work_order_lookup")));

        StringAssert.Contains(exception.Message, "connection timed out");
    }

    [TestMethod]
    public async Task BuildRefreshPayloadAsync_WhenTheQueryGenuinelyFails_SurfacesItInsteadOfSkipping()
    {
        var executor = new StubExecutor { Failure = "Invalid column name 'PART_ID'" };
        var source = new VisualShapePayloadSource(executor, NullLogger<VisualShapePayloadSource>.Instance);

        await Assert.ThrowsExceptionAsync<VisualReadFailedException>(
            () => source.BuildRefreshPayloadAsync(RequireShape("work_order_lookup")),
            "Only unreachability may be treated as a skip; a real query error must surface.");
    }

    [TestMethod]
    public async Task BuildRefreshPayloadAsync_WhenAShapeHasNoPopulationRead_ReportsASchemaMismatch()
    {
        var executor = new StubExecutor(Row(("NormalizedWorkOrder", "WO-1"), ("PartNumber", "P-1"), ("Description", "Ring"), ("WorkCenter", "WC-1")));
        var source = new VisualShapePayloadSource(executor, NullLogger<VisualShapePayloadSource>.Instance);

        var shapeWithoutPopulation = RequireShape("work_order_lookup") with { PopulationScriptRelativePath = null };

        await Assert.ThrowsExceptionAsync<VisualSourceSchemaMismatchException>(
            () => source.BuildRefreshPayloadAsync(shapeWithoutPopulation));
    }

    private static VisualReadShape RequireShape(string shapeKey) =>
        VisualReadShapeCatalog.FindByKey(shapeKey)
        ?? throw new InvalidOperationException($"The shipped catalog does not contain '{shapeKey}'.");

    private static IReadOnlyDictionary<string, object?> Row(params (string Key, object? Value)[] values) =>
        values.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

    /// <summary>Replays a scripted classified outcome and records what it was asked for.</summary>
    private sealed class StubExecutor : IVisualQueryExecutor
    {
        private readonly IReadOnlyList<IReadOnlyDictionary<string, object?>> _rows;

        public StubExecutor(params IReadOnlyDictionary<string, object?>[] rows) => _rows = rows;

        public string? Unreachable { get; init; }

        public string? Failure { get; init; }

        public string? LastScriptPath { get; private set; }

        public IReadOnlyDictionary<string, object?>? LastParameters { get; private set; }

        public Task<VisualQueryOutcome> ExecuteAsync(
            string sourceScriptRelativePath,
            IReadOnlyDictionary<string, object?> parameters,
            CancellationToken cancellationToken = default)
        {
            LastScriptPath = sourceScriptRelativePath;
            LastParameters = parameters;

            if (Unreachable is not null)
            {
                return Task.FromResult(VisualQueryOutcome.Unreachable(Unreachable));
            }

            if (Failure is not null)
            {
                return Task.FromResult(VisualQueryOutcome.Failed(Failure));
            }

            return Task.FromResult(VisualQueryOutcome.Ok(_rows));
        }
    }
}
