using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Services;

namespace MTM_Waitlist.Tests.Module_Mock;

/// <summary>
/// Verifies the fallback invariant for all five read shapes: a live answer always wins — including a
/// legitimately empty one — only unreachability reads the mirror, a genuine read failure surfaces, and
/// whichever source answers produces an identical row set (FR-004, FR-024, SC-004).
/// </summary>
[TestClass]
public sealed class VisualReadFallbackParityTests
{
    // ---- Shape 1: work_order_lookup ------------------------------------------------------------

    [TestMethod]
    public async Task WorkOrderLookup_Unreachable_ServesMirrorThroughShapeProcedure()
    {
        var cache = new RecordingMirrorCache([Row(("PartNumber", "P-1"), ("Description", "Ring"), ("WorkCenter", "WC-1"))]);
        var fallback = new VisualWorkOrderLookupFallback(new FakeVisualQueryExecutor(VisualQueryOutcome.Unreachable("down")), cache);

        var rows = await fallback.ReadAsync(new VisualWorkOrderLookupRequest("WO-1"));

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual("P-1", rows[0].PartNumber);
        Assert.AreEqual("Ring", rows[0].Description);
        Assert.AreEqual("WC-1", rows[0].WorkCenter);
        Assert.AreEqual("sp_visual_work_order_lookup_get", cache.LastProcedureName);
        Assert.AreEqual(MySqlDatabaseTarget.MtmMock, cache.LastTarget);
        Assert.AreEqual("WO-1", cache.LastParameters?["p_normalized_work_order"]);
    }

    [TestMethod]
    public async Task WorkOrderLookup_EmptyLiveAnswer_IsNotReplacedByCache()
    {
        var cache = new RecordingMirrorCache([Row(("PartNumber", "SHOULD-NOT-APPEAR"))]);
        var fallback = new VisualWorkOrderLookupFallback(new FakeVisualQueryExecutor(VisualQueryOutcome.Ok([])), cache);

        var result = await fallback.ReadWithProvenanceAsync(new VisualWorkOrderLookupRequest("WO-1"));

        Assert.AreEqual(0, result.Value.Count);
        Assert.AreEqual(VisualReadSource.ServedFromLive, result.Source);
        Assert.AreEqual(0, cache.ReadCount, "An empty live result is a real answer and must not consult the cache.");
    }

    // ---- Shape 2: operation_sequences ----------------------------------------------------------

    [TestMethod]
    public async Task OperationSequences_Unreachable_ServesMirrorThroughShapeProcedure()
    {
        var cache = new RecordingMirrorCache([Row(("SequenceNumber", "020"), ("Description", "Blank"))]);
        var fallback = new VisualOperationSequencesFallback(new FakeVisualQueryExecutor(VisualQueryOutcome.Unreachable("down")), cache);

        var rows = await fallback.ReadAsync(new VisualOperationSequenceRequest("WO-1", "P-1"));

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual("020", rows[0].SequenceNumber);
        Assert.AreEqual("Blank", rows[0].Description);
        Assert.AreEqual("sp_visual_operation_sequences_get", cache.LastProcedureName);
        Assert.AreEqual(MySqlDatabaseTarget.MtmMock, cache.LastTarget);
    }

    [TestMethod]
    public async Task OperationSequences_EmptyLiveAnswer_IsNotReplacedByCache()
    {
        var cache = new RecordingMirrorCache([Row(("SequenceNumber", "999"))]);
        var fallback = new VisualOperationSequencesFallback(new FakeVisualQueryExecutor(VisualQueryOutcome.Ok([])), cache);

        var result = await fallback.ReadWithProvenanceAsync(new VisualOperationSequenceRequest("WO-1", "P-1"));

        Assert.AreEqual(0, result.Value.Count);
        Assert.AreEqual(VisualReadSource.ServedFromLive, result.Source);
        Assert.AreEqual(0, cache.ReadCount);
    }

    // ---- Shape 3: subordinate_parts ------------------------------------------------------------

    [TestMethod]
    public async Task SubordinateParts_Unreachable_ServesMirrorThroughShapeProcedure()
    {
        var cache = new RecordingMirrorCache([
            Row(("Category", "Coil"), ("PartNumber", "SUB-1"), ("Description", "Sub"), ("Location", "V-A0-01"), ("User8", "U8"), ("OnHandQuantity", 12.5m))
        ]);
        var fallback = new VisualSubordinatePartsFallback(new FakeVisualQueryExecutor(VisualQueryOutcome.Unreachable("down")), cache);

        var rows = await fallback.ReadAsync(new VisualSubordinatePartRequest("WO-1", "P-1", "020"));

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual("Coil", rows[0].Category);
        Assert.AreEqual("SUB-1", rows[0].PartNumber);
        Assert.AreEqual(12.5m, rows[0].OnHandQuantity);
        Assert.AreEqual("sp_visual_subordinate_parts_get", cache.LastProcedureName);
        Assert.AreEqual("P-1", cache.LastParameters?["p_part_number"], "The operation's part is the mirror's parent part.");
        Assert.AreEqual(20, cache.LastParameters?["p_sequence_number"], "The text sequence must be converted to the mirror's integer.");
    }

    [TestMethod]
    public async Task SubordinateParts_EmptyLiveAnswer_IsNotReplacedByCache()
    {
        var cache = new RecordingMirrorCache([Row(("PartNumber", "SHOULD-NOT-APPEAR"))]);
        var fallback = new VisualSubordinatePartsFallback(new FakeVisualQueryExecutor(VisualQueryOutcome.Ok([])), cache);

        var result = await fallback.ReadWithProvenanceAsync(new VisualSubordinatePartRequest("WO-1", "P-1", "020"));

        Assert.AreEqual(0, result.Value.Count);
        Assert.AreEqual(VisualReadSource.ServedFromLive, result.Source);
        Assert.AreEqual(0, cache.ReadCount);
    }

    // ---- Shape 4: inventory_locations ----------------------------------------------------------

    [TestMethod]
    public async Task InventoryLocations_Unreachable_ServesMirrorThroughShapeProcedure()
    {
        var cache = new RecordingMirrorCache([Row(("PartNumber", "P-1"), ("Location", "V-A0-01"), ("OnHandQuantity", 46000m))]);
        var fallback = new VisualInventoryLocationsFallback(new FakeVisualQueryExecutor(VisualQueryOutcome.Unreachable("down")), cache);

        var rows = await fallback.ReadAsync(new VisualInventoryLocationRequest("P-1"));

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual("V-A0-01", rows[0].Location);
        Assert.AreEqual(46000m, rows[0].OnHandQuantity);
        Assert.AreEqual("sp_visual_inventory_locations_get", cache.LastProcedureName);
        Assert.AreEqual(MySqlDatabaseTarget.MtmMock, cache.LastTarget);
    }

    [TestMethod]
    public async Task InventoryLocations_EmptyLiveAnswer_IsNotReplacedByCache()
    {
        var cache = new RecordingMirrorCache([Row(("Location", "SHOULD-NOT-APPEAR"))]);
        var fallback = new VisualInventoryLocationsFallback(new FakeVisualQueryExecutor(VisualQueryOutcome.Ok([])), cache);

        var result = await fallback.ReadWithProvenanceAsync(new VisualInventoryLocationRequest("P-1"));

        Assert.AreEqual(0, result.Value.Count);
        Assert.AreEqual(VisualReadSource.ServedFromLive, result.Source);
        Assert.AreEqual(0, cache.ReadCount);
    }

    // ---- Shape 5: disposition_input ------------------------------------------------------------

    [TestMethod]
    public async Task DispositionInput_Unreachable_ServesMirrorThroughShapeProcedure()
    {
        var cache = new RecordingMirrorCache([
            Row(("WorkOrderStatus", "R"), ("OpenWorkOrderQuantity", 5m), ("FinishedGoodsQuantity", 2m), ("HasOutsideVendorOperation", true))
        ]);
        var fallback = new VisualDispositionInputFallback(new FakeVisualQueryExecutor(VisualQueryOutcome.Unreachable("down")), cache);

        var rows = await fallback.ReadAsync(new VisualDispositionInputRequest("WO-1", "P-1"));

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual("R", rows[0].WorkOrderStatus);
        Assert.AreEqual(5m, rows[0].OpenWorkOrderQuantity);
        Assert.IsTrue(rows[0].HasOutsideVendorOperation);
        Assert.AreEqual("sp_visual_disposition_input_get", cache.LastProcedureName);
        Assert.AreEqual("WO-1", cache.LastParameters?["p_work_order"]);
    }

    [TestMethod]
    public async Task DispositionInput_EmptyLiveAnswer_IsNotReplacedByCache()
    {
        var cache = new RecordingMirrorCache([Row(("WorkOrderStatus", "R"))]);
        var fallback = new VisualDispositionInputFallback(new FakeVisualQueryExecutor(VisualQueryOutcome.Ok([])), cache);

        var result = await fallback.ReadWithProvenanceAsync(new VisualDispositionInputRequest("WO-1", "P-1"));

        Assert.AreEqual(0, result.Value.Count);
        Assert.AreEqual(VisualReadSource.ServedFromLive, result.Source);
        Assert.AreEqual(0, cache.ReadCount);
    }

    // ---- Cross-cutting ------------------------------------------------------------------------

    [TestMethod]
    public async Task FailedLiveRead_SurfacesInsteadOfReadingTheCache()
    {
        var cache = new RecordingMirrorCache([Row(("PartNumber", "SHOULD-NOT-APPEAR"))]);
        var fallback = new VisualWorkOrderLookupFallback(
            new FakeVisualQueryExecutor(VisualQueryOutcome.Failed("SQL error 102")),
            cache);

        var exception = await Assert.ThrowsExceptionAsync<VisualReadFailedException>(
            () => fallback.ReadAsync(new VisualWorkOrderLookupRequest("WO-1")));

        Assert.AreEqual("work_order_lookup", exception.ShapeKey);
        Assert.AreEqual(0, cache.ReadCount, "Only unreachability may consult the cache.");
    }

    [TestMethod]
    public async Task UnreachableWithoutCacheConfigured_SurfacesInsteadOfReturningNothing()
    {
        var fallback = new VisualWorkOrderLookupFallback(
            new FakeVisualQueryExecutor(VisualQueryOutcome.Unreachable("down")),
            mySqlHelperServer: null);

        await Assert.ThrowsExceptionAsync<VisualReadFailedException>(
            () => fallback.ReadAsync(new VisualWorkOrderLookupRequest("WO-1")));
    }

    [TestMethod]
    public async Task LiveRead_IsAttemptedWithTheShapesOwnScript()
    {
        var executor = new FakeVisualQueryExecutor(VisualQueryOutcome.Ok([]));
        var fallback = new VisualWorkOrderLookupFallback(executor, new RecordingMirrorCache([]));

        _ = await fallback.ReadAsync(new VisualWorkOrderLookupRequest("WO-1"));

        Assert.AreEqual("Database/InforVisual/Queues/Module_Setup/Queries/LookupWorkOrder.sql", executor.LastScriptPath);
        Assert.AreEqual("WO-1", executor.LastParameters?["NormalizedWorkOrder"]);
        Assert.AreEqual(1, executor.ExecuteCount, "The live read is attempted exactly once per read.");
    }

    [TestMethod]
    public async Task LiveAndCached_ProduceIdenticalRows()
    {
        // Every shape maps both sources through the same row reader, so identical columns yield
        // identical rows and a caller cannot tell which source answered (FR-004).
        var liveRows = new[] { Row(("PartNumber", "P-1"), ("Description", "Ring"), ("WorkCenter", "WC-1")) };
        var cachedRows = new[] { Row(("PartNumber", "P-1"), ("Description", "Ring"), ("WorkCenter", "WC-1")) };

        var live = await new VisualWorkOrderLookupFallback(
                new FakeVisualQueryExecutor(VisualQueryOutcome.Ok(liveRows)), new RecordingMirrorCache([]))
            .ReadAsync(new VisualWorkOrderLookupRequest("WO-1"));

        var cached = await new VisualWorkOrderLookupFallback(
                new FakeVisualQueryExecutor(VisualQueryOutcome.Unreachable("down")), new RecordingMirrorCache(cachedRows))
            .ReadAsync(new VisualWorkOrderLookupRequest("WO-1"));

        Assert.AreEqual(live.Count, cached.Count);
        for (var index = 0; index < live.Count; index++)
        {
            Assert.AreEqual(live[index], cached[index]);
        }
    }

    private static Dictionary<string, object?> Row(params (string Key, object? Value)[] values)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values)
        {
            row[key] = value;
        }

        return row;
    }
}
