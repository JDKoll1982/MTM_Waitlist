using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Mock.Contracts;
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

    [TestMethod]
    public void TheReadSeam_CannotTellACallerWhichSourceAnswered()
    {
        // FR-004's structural identity is only guaranteed if the seam a caller uses exposes no provenance at
        // all: ReadAsync returns the row list, and only the opt-in ReadWithProvenanceAsync reports the source.
        var readAsync = typeof(IVisualReadFallback<,>).GetMethod("ReadAsync");
        Assert.IsNotNull(readAsync);
        Assert.AreEqual(
            typeof(Task<>),
            readAsync.ReturnType.GetGenericTypeDefinition(),
            "ReadAsync must return only the rows.");
        Assert.AreEqual(
            typeof(IReadOnlyList<>),
            readAsync.ReturnType.GetGenericArguments()[0].GetGenericTypeDefinition(),
            "ReadAsync's result must be a plain row list — not a wrapper that could carry the source with it.");

        var provenanceAware = typeof(IVisualReadFallback<,>)
            .GetMethods()
            .Where(method => method.Name.Contains("Provenance", StringComparison.Ordinal))
            .ToArray();
        Assert.AreEqual(
            1,
            provenanceAware.Length,
            "Provenance is opt-in through exactly one method, so a normal caller cannot ask for it by accident.");
    }

    /// <summary>
    /// FR-027: the mirror cache is the Infor Visual cache and nothing else. An internal-store read must never be
    /// able to reach it, which is true by construction only while no fallback code names an internal target.
    /// </summary>
    [TestMethod]
    public void NoFallbackCode_PointsAtAnInternalStore()
    {
        var mockLibrary = Path.Combine(FindRepositoryRoot(), "MTM_Waitlist.Mock");
        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(mockLibrary, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            foreach (var internalTarget in new[]
                     {
                         "MySqlDatabaseTarget.MtmWaitlist",
                         "MySqlDatabaseTarget.MtmReceivingApplication",
                         "MySqlDatabaseTarget.MtmWipApplication",
                     })
            {
                if (content.Contains(internalTarget, StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetRelativePath(FindRepositoryRoot(), file)}: {internalTarget}");
                }
            }
        }

        Assert.AreEqual(
            0,
            violations.Count,
            "The cache exists for Infor Visual reads only (FR-027); no fallback code may name an internal store:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// A live script receives its inputs by name, so a name the script does not declare is not a build
    /// error: the server rejects the batch with SQL error 137 ("Must declare the scalar variable @X")
    /// and the read returns nothing. Shape 3 <c>subordinate_parts</c> shipped exactly that way, which is
    /// why subordinate parts silently loaded as an empty set. This derives the required names from each
    /// script and compares them with what the fallback actually passes, so a renamed script parameter
    /// can never quietly empty a read again.
    /// </summary>
    [TestMethod]
    public async Task EveryShape_SuppliesEveryParameterItsLiveScriptDeclares()
    {
        var repositoryRoot = FindRepositoryRoot();

        var cases = new (string ShapeKey, Func<FakeVisualQueryExecutor, Task> Run)[]
        {
            ("work_order_lookup", executor => new VisualWorkOrderLookupFallback(executor).ReadAsync(new VisualWorkOrderLookupRequest("WO-1"))),
            ("operation_sequences", executor => new VisualOperationSequencesFallback(executor).ReadAsync(new VisualOperationSequenceRequest("WO-1", "P-1"))),
            ("subordinate_parts", executor => new VisualSubordinatePartsFallback(executor).ReadAsync(new VisualSubordinatePartRequest("WO-1", "P-1", "020"))),
            ("inventory_locations", executor => new VisualInventoryLocationsFallback(executor).ReadAsync(new VisualInventoryLocationRequest("P-1"))),
            ("disposition_input", executor => new VisualDispositionInputFallback(executor).ReadAsync(new VisualDispositionInputRequest("WO-1", "P-1"))),
        };

        var violations = new List<string>();

        foreach (var (shapeKey, run) in cases)
        {
            var executor = new FakeVisualQueryExecutor(VisualQueryOutcome.Ok([]));
            await run(executor).ConfigureAwait(false);

            var shape = VisualReadShapeCatalog.FindByKey(shapeKey);
            Assert.IsNotNull(shape, $"Shape '{shapeKey}' is not registered in the shape catalog.");

            var scriptPath = Path.Combine(
                repositoryRoot,
                shape!.SourceScriptRelativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.IsTrue(File.Exists(scriptPath), $"The live script for '{shapeKey}' was not found at '{scriptPath}'.");

            var supplied = (executor.LastParameters?.Keys ?? [])
                .Select(name => name.StartsWith('@') ? name : $"@{name}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var required in ReadExternalParameters(File.ReadAllText(scriptPath)))
            {
                if (!supplied.Contains(required))
                {
                    violations.Add(
                        $"{shapeKey}: {Path.GetFileName(scriptPath)} declares {required}, but the fallback supplies "
                        + $"[{string.Join(", ", executor.LastParameters?.Keys ?? [])}]");
                }
            }
        }

        Assert.AreEqual(
            0,
            violations.Count,
            "Every parameter a live script uses must be supplied by its fallback; an unmatched name fails at the "
                + "server as SQL error 137 and the read silently returns nothing:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// Reads the names a script expects from outside: every <c>@name</c> token it does not declare
    /// itself. Comments are stripped first, so documentation examples are not mistaken for parameters.
    /// </summary>
    private static IReadOnlyList<string> ReadExternalParameters(string script)
    {
        var withoutBlockComments = Regex.Replace(script, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        var code = Regex.Replace(withoutBlockComments, "--.*$", string.Empty, RegexOptions.Multiline);

        var declared = Regex.Matches(code, @"\bDECLARE\s+(@[A-Za-z0-9_]+)", RegexOptions.IgnoreCase)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Regex.Matches(code, "(@[A-Za-z0-9_]+)")
            .Select(match => match.Groups[1].Value)
            .Where(name => !declared.Contains(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MTM_Waitlist.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail($"The repository root could not be located above '{AppContext.BaseDirectory}'.");
        return AppContext.BaseDirectory;
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
