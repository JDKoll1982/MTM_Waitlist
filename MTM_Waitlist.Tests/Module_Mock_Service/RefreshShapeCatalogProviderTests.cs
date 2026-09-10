using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Service.Services;
using MTM_Waitlist.Mock.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// Verifies the shape-catalog startup validation: artifact existence, mirror-projection agreement,
/// and — critically — that a bad or unreachable shape is reported and excluded rather than crashing
/// the service or stopping the other shapes (FR-020, constitution III).
/// </summary>
[TestClass]
public sealed class RefreshShapeCatalogProviderTests
{
    /// <summary>Full physical column list for the <c>work_order_lookup</c> mirror.</summary>
    private const string WorkOrderLookupColumns =
        "id,normalized_work_order,part_number,description,work_center,refreshed_utc,is_seed_content";

    /// <summary>
    /// The mirror's physical columns for each shipped shape — what a correctly deployed cache reports.
    /// </summary>
    private static string ColumnsFor(string shapeKey) => shapeKey switch
    {
        "work_order_lookup" => WorkOrderLookupColumns,
        "operation_sequences" =>
            "id,normalized_work_order,part_number,sequence_number,description,refreshed_utc,is_seed_content",
        "subordinate_parts" =>
            "id,normalized_work_order,parent_part_number,sequence_number,category,part_number,description," +
            "location,user8,on_hand_quantity,refreshed_utc,is_seed_content",
        "inventory_locations" =>
            "id,part_number,location,on_hand_quantity,refreshed_utc,is_seed_content",
        "disposition_input" =>
            "id,work_order,part_number,work_order_status,open_work_order_quantity,finished_goods_quantity," +
            "has_outside_vendor_operation,refreshed_utc,is_seed_content",
        _ => WorkOrderLookupColumns
    };

    [TestMethod]
    public async Task ValidateAsync_WithAgreeingArtifacts_MarksEveryShapeValid()
    {
        var provider = CreateProvider(key => Metadata(ColumnsFor(key)));

        var entries = await provider.ValidateAsync();

        Assert.AreEqual(5, entries.Count, "The shipped catalog describes five shapes.");
        Assert.IsTrue(entries.All(entry => entry.IsValid), "Every shape should validate against matching metadata.");
        Assert.AreEqual(5, provider.RefreshableShapes.Count);
        Assert.AreEqual(0, provider.InvalidShapes.Count);
    }

    [TestMethod]
    public async Task ValidateAsync_WhenArtifactsAreMissing_ReportsAndExcludesThatShapeOnly()
    {
        var provider = CreateProvider(
            key => Metadata(ColumnsFor(key)),
            overrides: new() { ["operation_sequences"] = Metadata(WorkOrderLookupColumns, hasStage: false) });

        var entries = await provider.ValidateAsync();

        Assert.AreEqual(4, provider.RefreshableShapes.Count, "Only the broken shape should be excluded.");
        Assert.AreEqual(1, provider.InvalidShapes.Count);

        var invalid = provider.InvalidShapes[0];
        Assert.AreEqual("operation_sequences", invalid.Shape.Key);
        StringAssert.Contains(invalid.InvalidReason, "Missing artifacts");
        Assert.IsFalse(entries.Single(entry => entry.Shape.Key == "operation_sequences").IsValid);
    }

    [TestMethod]
    public async Task ValidateAsync_WhenTheMirrorProjectionDrifts_ReportsAColumnMismatch()
    {
        var provider = CreateProvider(
            key => key == "work_order_lookup"
                ? Metadata("id,normalized_work_order,part_number,description,refreshed_utc,is_seed_content")
                : Metadata(ColumnsFor(key)));

        await provider.ValidateAsync();

        Assert.AreEqual(1, provider.InvalidShapes.Count);
        StringAssert.Contains(provider.InvalidShapes[0].InvalidReason, "Mirror column mismatch");
        StringAssert.Contains(provider.InvalidShapes[0].InvalidReason, "work_center");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenTheMetadataReadThrows_TreatsTheShapeAsInvalidInsteadOfCrashing()
    {
        var provider = CreateProvider(key => key == "disposition_input"
            ? throw new InvalidOperationException("cache unreachable")
            : Metadata(ColumnsFor(key)));

        var entries = await provider.ValidateAsync();

        Assert.AreEqual(5, entries.Count, "Validation must still report every catalog shape.");
        Assert.AreEqual(1, provider.InvalidShapes.Count);
        Assert.AreEqual("disposition_input", provider.InvalidShapes[0].Shape.Key);
        StringAssert.Contains(provider.InvalidShapes[0].InvalidReason, "cache unreachable");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenTheSourceScriptIsAbsent_ReportsTheMissingScript()
    {
        var provider = CreateProvider(
            key => Metadata(ColumnsFor(key)),
            catalog:
            [
                new VisualReadShape
                {
                    Key = "work_order_lookup",
                    Module = VisualReadShapeModule.ModuleSetup,
                    SourceScriptRelativePath = "Database/InforVisual/Queues/Module_Setup/Queries/DoesNotExist.sql",
                    OutputColumns = [new VisualShapeColumn("PartNumber", "nvarchar(60)")]
                }
            ]);

        await provider.ValidateAsync();

        Assert.AreEqual(1, provider.InvalidShapes.Count);
        StringAssert.Contains(provider.InvalidShapes[0].InvalidReason, "Source script not found");
    }

    [TestMethod]
    public async Task ValidateAsync_ExcludesDisabledShapesFromTheRefreshableWorkList()
    {
        var catalog = VisualReadShapeCatalog.Create()
            .Select(shape => shape.Key == "inventory_locations" ? shape with { IsEnabled = false } : shape)
            .ToList();

        var provider = CreateProvider(key => Metadata(ColumnsFor(key)), catalog: catalog);

        await provider.ValidateAsync();

        Assert.AreEqual(0, provider.InvalidShapes.Count, "A parked shape is valid, not broken.");
        Assert.AreEqual(4, provider.RefreshableShapes.Count, "A parked shape must not be refreshed.");
        Assert.AreEqual(1, provider.DisabledShapes.Count);
        Assert.AreEqual("inventory_locations", provider.DisabledShapes[0].Key);
    }

    [TestMethod]
    public async Task ValidateAsync_ReportsNoEntriesBeforeValidationRuns()
    {
        var provider = CreateProvider(key => Metadata(ColumnsFor(key)));

        Assert.AreEqual(0, provider.Entries.Count, "The work list must be empty until validation has run.");

        await provider.ValidateAsync();

        Assert.AreEqual(5, provider.Entries.Count);
    }

    private static VisualShapeMetadata Metadata(
        string mirrorColumns,
        bool hasMirror = true,
        bool hasStage = true,
        bool hasGet = true,
        bool hasRefresh = true) =>
        new(
            "shape",
            hasMirror ? 1 : 0,
            hasStage ? 1 : 0,
            hasGet ? 1 : 0,
            hasRefresh ? 1 : 0,
            hasMirror ? mirrorColumns : null);

    private static RefreshShapeCatalogProvider CreateProvider(
        Func<string, VisualShapeMetadata?> metadataFactory,
        Dictionary<string, VisualShapeMetadata>? overrides = null,
        IReadOnlyList<VisualReadShape>? catalog = null) =>
        new(
            new StubMetadataReader(metadataFactory, overrides),
            catalog,
            contentRoot: AppContext.BaseDirectory);

    /// <summary>
    /// Test double for <see cref="IVisualShapeMetadataReader"/>. Returns per-key overrides when
    /// supplied, otherwise delegates to the factory so a test can also simulate a thrown read.
    /// </summary>
    private sealed class StubMetadataReader : IVisualShapeMetadataReader
    {
        private readonly Func<string, VisualShapeMetadata?> _factory;
        private readonly Dictionary<string, VisualShapeMetadata> _overrides;

        public StubMetadataReader(
            Func<string, VisualShapeMetadata?> factory,
            Dictionary<string, VisualShapeMetadata>? overrides)
        {
            _factory = factory;
            _overrides = overrides ?? [];
        }

        public Task<VisualShapeMetadata?> GetShapeMetadataAsync(
            string shapeKey,
            CancellationToken cancellationToken = default)
        {
            if (_overrides.TryGetValue(shapeKey, out var metadata))
            {
                return Task.FromResult<VisualShapeMetadata?>(metadata with { ShapeKey = shapeKey });
            }

            var result = _factory(shapeKey);
            return Task.FromResult(result is null ? null : result with { ShapeKey = shapeKey });
        }
    }
}
