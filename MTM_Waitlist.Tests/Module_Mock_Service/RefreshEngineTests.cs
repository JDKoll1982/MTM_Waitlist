using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Service.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// Verifies the refresh engine's cycle semantics: a skipped (unreachable-source) or failed refresh
/// must leave the live snapshot untouched — the mirror writer is never invoked — while a successful
/// cycle records the rows it swapped in (FR-008, FR-006).
/// </summary>
[TestClass]
public sealed class RefreshEngineTests
{
    private static readonly IReadOnlyList<VisualReadShape> TwoShapeCatalog =
    [
        new VisualReadShape
        {
            Key = "work_order_lookup",
            Module = VisualReadShapeModule.ModuleSetup,
            SourceScriptRelativePath = "Database/InforVisual/Queues/Module_Setup/Queries/LookupWorkOrder.sql",
            InputParameters = [new VisualShapeParameter("NormalizedWorkOrder", "nvarchar(30)", true)],
            OutputColumns = [new VisualShapeColumn("PartNumber", "nvarchar(60)")]
        },
        new VisualReadShape
        {
            Key = "operation_sequences",
            Module = VisualReadShapeModule.ModuleSetup,
            SourceScriptRelativePath = "Database/InforVisual/Queues/Module_Setup/Queries/GetSequences.sql",
            InputParameters = [new VisualShapeParameter("NormalizedWorkOrder", "nvarchar(30)", true)],
            OutputColumns = [new VisualShapeColumn("SequenceNumber", "int")]
        }
    ];

    [TestMethod]
    public async Task RefreshShapeAsync_OnSuccess_RecordsTheRowsSwappedIn()
    {
        var writer = new StubMirrorWriter { RowsToReport = 7 };
        var engine = CreateEngine(new StubPayloadSource(), writer);

        var record = await engine.RefreshShapeAsync(TwoShapeCatalog[0]);

        Assert.AreEqual(RefreshRunOutcome.Succeeded, record.Outcome);
        Assert.AreEqual(7, record.RowCount);
        Assert.AreEqual("work_order_lookup", record.ShapeKey);
        Assert.IsNotNull(record.FinishedUtc);
        Assert.AreEqual(1, writer.CallCount);
    }

    [TestMethod]
    public async Task RefreshShapeAsync_WhenTheSourceIsUnreachable_SkipsAndLeavesTheSnapshotUntouched()
    {
        var writer = new StubMirrorWriter();
        var engine = CreateEngine(
            new StubPayloadSource
            {
                ExceptionToThrow = new VisualSourceUnreachableException("Could not connect to VISUAL.")
            },
            writer);

        var record = await engine.RefreshShapeAsync(TwoShapeCatalog[0]);

        Assert.AreEqual(RefreshRunOutcome.SkippedSourceUnreachable, record.Outcome);
        Assert.IsNull(record.RowCount);
        Assert.AreEqual(0, writer.CallCount, "A skipped refresh must not touch the live mirror.");
    }

    [TestMethod]
    public async Task RefreshShapeAsync_WhenTheWriteFails_RecordsAFailureWithoutRethrowing()
    {
        var writer = new StubMirrorWriter { ExceptionToThrow = new InvalidOperationException("load rejected") };
        var engine = CreateEngine(new StubPayloadSource(), writer);

        var record = await engine.RefreshShapeAsync(TwoShapeCatalog[0]);

        Assert.AreEqual(RefreshRunOutcome.FailedUnknown, record.Outcome);
        StringAssert.Contains(record.ErrorMessage, "load rejected");
    }

    [TestMethod]
    public async Task RefreshShapeAsync_RedactsCredentialMaterialFromRecordedErrors()
    {
        var engine = CreateEngine(
            new StubPayloadSource
            {
                ExceptionToThrow = new VisualSourceUnreachableException(
                    "Login failed for Server=visual;User ID=shop2;Password=hunter2;Database=MTMFG;")
            },
            new StubMirrorWriter());

        var record = await engine.RefreshShapeAsync(TwoShapeCatalog[0]);

        Assert.IsNotNull(record.ErrorMessage);
        Assert.IsFalse(
            record.ErrorMessage.Contains("hunter2", StringComparison.Ordinal),
            "FR-026: a credential must never be recorded in an error message.");
    }

    [TestMethod]
    public async Task RefreshShapeAsync_RaisesRunCompleted()
    {
        var engine = CreateEngine(new StubPayloadSource(), new StubMirrorWriter());
        RefreshRunRecord? observed = null;
        engine.RunCompleted += (_, record) => observed = record;

        var record = await engine.RefreshShapeAsync(TwoShapeCatalog[0]);

        Assert.AreSame(record, observed, "The run record must be published for the run-record store.");
    }

    [TestMethod]
    public async Task RefreshCycleAsync_VisitsEveryRefreshableShape()
    {
        var writer = new StubMirrorWriter { RowsToReport = 1 };
        var engine = CreateEngine(new StubPayloadSource(), writer, TwoShapeCatalog);

        var records = await engine.RefreshCycleAsync();

        Assert.AreEqual(2, records.Count);
        Assert.AreEqual(2, writer.CallCount);
        Assert.IsTrue(records.All(record => record.Outcome == RefreshRunOutcome.Succeeded));
        Assert.AreEqual(2, engine.LastRunRecords.Count);
    }

    [TestMethod]
    public async Task RefreshCycleAsync_SkipsShapesThatFailedCatalogValidation()
    {
        var writer = new StubMirrorWriter();
        var catalogProvider = new RefreshShapeCatalogProvider(
            // The stub reports artifacts that never match, so every shape fails validation.
            new RejectingMetadataReader(),
            TwoShapeCatalog,
            contentRoot: AppContext.BaseDirectory);

        var engine = new RefreshEngine(
            catalogProvider,
            new StubPayloadSource(),
            writer,
            NullLogger<RefreshEngine>.Instance);

        await catalogProvider.ValidateAsync();
        var records = await engine.RefreshCycleAsync();

        Assert.AreEqual(0, records.Count, "An invalid catalog must produce no refresh work.");
        Assert.AreEqual(0, writer.CallCount);
    }

    private static RefreshEngine CreateEngine(
        IVisualShapePayloadSource payloadSource,
        IMockMirrorRefreshWriter writer,
        IReadOnlyList<VisualReadShape>? catalog = null)
    {
        var shapes = catalog ?? TwoShapeCatalog;
        var catalogProvider = new RefreshShapeCatalogProvider(
            new AcceptingMetadataReader(shapes),
            shapes,
            contentRoot: AppContext.BaseDirectory);

        catalogProvider.ValidateAsync().GetAwaiter().GetResult();

        return new RefreshEngine(
            catalogProvider,
            payloadSource,
            writer,
            NullLogger<RefreshEngine>.Instance);
    }

    /// <summary>Payload source stub that either returns a fixed payload or throws a configured exception.</summary>
    private sealed class StubPayloadSource : IVisualShapePayloadSource
    {
        public Exception? ExceptionToThrow { get; init; }

        public string Payload { get; init; } = "[]";

        public Task<string> BuildRefreshPayloadAsync(
            VisualReadShape shape,
            CancellationToken cancellationToken = default) =>
            ExceptionToThrow is not null
                ? Task.FromException<string>(ExceptionToThrow)
                : Task.FromResult(Payload);
    }

    /// <summary>Mirror writer stub that counts calls and can be told to fail.</summary>
    private sealed class StubMirrorWriter : IMockMirrorRefreshWriter
    {
        public int RowsToReport { get; init; }

        public Exception? ExceptionToThrow { get; init; }

        public int CallCount { get; private set; }

        public Task<int> RefreshAsync(
            VisualReadShape shape,
            string jsonPayload,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            return ExceptionToThrow is not null
                ? Task.FromException<int>(ExceptionToThrow)
                : Task.FromResult(RowsToReport);
        }
    }

    /// <summary>Reports a mirror whose columns match the shape, so validation succeeds.</summary>
    private sealed class AcceptingMetadataReader : IVisualShapeMetadataReader
    {
        private readonly IReadOnlyList<VisualReadShape> _shapes;

        public AcceptingMetadataReader(IReadOnlyList<VisualReadShape> shapes) => _shapes = shapes;

        public Task<VisualShapeMetadata?> GetShapeMetadataAsync(
            string shapeKey,
            CancellationToken cancellationToken = default)
        {
            var shape = _shapes.First(candidate => candidate.Key == shapeKey);

            var columns = new List<string> { "id" };
            columns.AddRange(shape.InputParameters.Select(parameter => ToSnakeCase(parameter.Name)));
            columns.AddRange(shape.OutputColumns.Select(column => ToSnakeCase(column.Name)));
            columns.Add("refreshed_utc");
            columns.Add("is_seed_content");

            return Task.FromResult<VisualShapeMetadata?>(
                new VisualShapeMetadata(shapeKey, 1, 1, 1, 1, string.Join(",", columns.Distinct(StringComparer.OrdinalIgnoreCase))));
        }
    }

    /// <summary>Reports a mirror whose columns never match, so every shape fails validation.</summary>
    private sealed class RejectingMetadataReader : IVisualShapeMetadataReader
    {
        public Task<VisualShapeMetadata?> GetShapeMetadataAsync(
            string shapeKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<VisualShapeMetadata?>(
                new VisualShapeMetadata(shapeKey, 1, 1, 1, 1, "id,unexpected_column"));
    }

    private static string ToSnakeCase(string value)
    {
        var builder = new System.Text.StringBuilder(value.Length + 8);

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];

            if (char.IsUpper(character))
            {
                if (index > 0)
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(character));
            }
            else
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
