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
            // Catalog validation requires a population read to exist on disk, so the synthetic shapes point at
            // the shipped one; the engine tests never execute it (the payload source is stubbed).
            PopulationScriptRelativePath = "Database/InforVisual/Queues/Module_Mock/Populations/work_order_lookup_population.sql",
            InputParameters = [new VisualShapeParameter("NormalizedWorkOrder", "nvarchar(30)", true)],
            OutputColumns = [new VisualShapeColumn("PartNumber", "nvarchar(60)")]
        },
        new VisualReadShape
        {
            Key = "operation_sequences",
            Module = VisualReadShapeModule.ModuleSetup,
            SourceScriptRelativePath = "Database/InforVisual/Queues/Module_Setup/Queries/GetSequences.sql",
            PopulationScriptRelativePath = "Database/InforVisual/Queues/Module_Mock/Populations/operation_sequences_population.sql",
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

    [TestMethod]
    public void GetInterval_PrefersTheShapeOverrideOverTheGlobalDefault()
    {
        var engine = CreateEngine(new StubPayloadSource(), new StubMirrorWriter(), TwoShapeCatalog, TimeSpan.FromMinutes(5));
        var withOverride = TwoShapeCatalog[0] with { RefreshIntervalOverride = TimeSpan.FromMinutes(1) };

        Assert.AreEqual(TimeSpan.FromMinutes(1), engine.GetInterval(withOverride));
        Assert.AreEqual(TimeSpan.FromMinutes(5), engine.GetInterval(TwoShapeCatalog[1]));
    }

    [TestMethod]
    public async Task TryRunDueShapesAsync_RunsEveryShapeOnce_ThenWaitsForTheInterval()
    {
        var writer = new StubMirrorWriter { RowsToReport = 2 };
        var engine = CreateEngine(new StubPayloadSource(), writer, TwoShapeCatalog, TimeSpan.FromMinutes(30));

        var first = await engine.TryRunDueShapesAsync(DateTime.UtcNow);

        Assert.AreEqual(2, first!.Count);
        Assert.AreEqual(2, writer.CallCount);

        var second = await engine.TryRunDueShapesAsync(DateTime.UtcNow);

        Assert.AreEqual(0, second!.Count, "A shape must not be refreshed again before its interval has elapsed.");
        Assert.AreEqual(2, writer.CallCount);
    }

    [TestMethod]
    public async Task TryRunDueShapesAsync_RerunsTheShapeOnceTheIntervalHasElapsed()
    {
        var now = DateTime.UtcNow;
        var writer = new StubMirrorWriter { RowsToReport = 1 };
        var engine = CreateEngine(new StubPayloadSource(), writer, TwoShapeCatalog, TimeSpan.FromMinutes(10));

        await engine.TryRunDueShapesAsync(now);
        var later = await engine.TryRunDueShapesAsync(now + TimeSpan.FromMinutes(11));

        Assert.AreEqual(2, later!.Count);
        Assert.AreEqual(4, writer.CallCount);
    }

    [TestMethod]
    public async Task TryRunCycleAsync_WhileACycleIsRunning_IsRejectedInsteadOfOverlapped()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writer = new StubMirrorWriter { RowsToReport = 1 };
        var engine = CreateEngine(new StubPayloadSource { Gate = gate }, writer);

        var inFlight = engine.TryRunCycleAsync();
        await WaitUntilAsync(() => engine.IsCycleRunning);

        var overlapping = await engine.TryRunCycleAsync();

        Assert.IsNull(overlapping, "One cycle at a time: a second request must be rejected rather than overlapped.");

        gate.SetResult();

        var completed = await inFlight;
        Assert.AreEqual(2, completed!.Count);
        Assert.AreEqual(2, writer.CallCount);
        Assert.IsFalse(engine.IsCycleRunning);
    }

    [TestMethod]
    public async Task RunScheduledAsync_KeepsRefreshingAfterAnUnreachableSourceCycle()
    {
        var payloadSource = new StubPayloadSource
        {
            ExceptionForCall = call => call == 1 ? new VisualSourceUnreachableException("Could not connect to VISUAL.") : null
        };
        var writer = new StubMirrorWriter { RowsToReport = 1 };
        var engine = CreateEngine(
            payloadSource,
            writer,
            TwoShapeCatalog,
            refreshInterval: TimeSpan.FromMilliseconds(20),
            minimumScheduledWait: TimeSpan.FromMilliseconds(5));

        var observed = new List<RefreshRunRecord>();
        engine.RunCompleted += (_, record) =>
        {
            lock (observed)
            {
                observed.Add(record);
            }
        };

        using var cts = new CancellationTokenSource();
        var loop = engine.RunScheduledAsync(TimeProvider.System, cts.Token);

        await WaitUntilAsync(() => writer.CallCount >= 2, TimeSpan.FromSeconds(10));
        await cts.CancelAsync();
        await loop;

        RefreshRunRecord[] snapshot;
        lock (observed)
        {
            snapshot = observed.ToArray();
        }

        var skipIndex = Array.FindIndex(snapshot, record => record.Outcome == RefreshRunOutcome.SkippedSourceUnreachable);
        Assert.IsTrue(skipIndex >= 0, "An unreachable source must be recorded as a normal skipped outcome (FR-008).");
        Assert.IsTrue(
            snapshot.Skip(skipIndex + 1).Any(record => record.Outcome == RefreshRunOutcome.Succeeded),
            "The scheduled loop must attempt the next cycle after a skipped one (US3 acceptance 3).");
        Assert.IsTrue(writer.CallCount >= 2, "A skipped cycle must not stop later refreshes.");
    }

    [TestMethod]
    public async Task ScheduledRefresh_LandsOnTheThreeHourLocalScheduleAsync()
    {
        // 04:30 local (UTC-6) -> the next grid slot is 06:00 local = 12:00 UTC.
        var zone = TimeZoneInfo.CreateCustomTimeZone("test-utc-minus-6", TimeSpan.FromHours(-6), "Test UTC-6", "Test UTC-6");
        var utcNow = new DateTimeOffset(2026, 9, 10, 10, 30, 0, TimeSpan.Zero);
        var writer = new StubMirrorWriter { RowsToReport = 1 };
        var engine = CreateEngine(
            new StubPayloadSource(),
            writer,
            TwoShapeCatalog,
            refreshInterval: TimeSpan.FromHours(3),
            timeProvider: new FixedTimeProvider(utcNow, zone));

        var first = await engine.TryRunDueShapesAsync(utcNow.UtcDateTime);
        Assert.AreEqual(2, first!.Count);

        // 04:35 local is not a slot, so nothing is due.
        var betweenSlots = await engine.TryRunDueShapesAsync(utcNow.UtcDateTime.AddMinutes(5));
        Assert.AreEqual(0, betweenSlots!.Count, "The refresh must wait for the next 3-hour slot, not run 3 hours after the last one.");

        // 06:00 local is a slot.
        var atSlot = await engine.TryRunDueShapesAsync(new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc));
        Assert.AreEqual(2, atSlot!.Count);

        // and the following slot is 09:00 local = 15:00 UTC.
        var beforeNextSlot = await engine.TryRunDueShapesAsync(new DateTime(2026, 9, 10, 14, 59, 0, DateTimeKind.Utc));
        Assert.AreEqual(0, beforeNextSlot!.Count);

        var nextSlot = await engine.TryRunDueShapesAsync(new DateTime(2026, 9, 10, 15, 0, 0, DateTimeKind.Utc));
        Assert.AreEqual(2, nextSlot!.Count);
    }

    [TestMethod]
    public async Task ScheduledRefresh_UsesTheServicesGlobalIntervalWhenAShapeDoesNotOverrideItAsync()
    {
        // A shape override is honoured as its own grid: 1 hour lands on the top of each hour.
        var zone = TimeZoneInfo.CreateCustomTimeZone("test-utc-minus-6", TimeSpan.FromHours(-6), "Test UTC-6", "Test UTC-6");
        var utcNow = new DateTimeOffset(2026, 9, 10, 10, 30, 0, TimeSpan.Zero); // 04:30 local
        var withOverride = TwoShapeCatalog[0] with { RefreshIntervalOverride = TimeSpan.FromHours(1) };
        var writer = new StubMirrorWriter { RowsToReport = 1 };
        var engine = CreateEngine(
            new StubPayloadSource(),
            writer,
            [withOverride, TwoShapeCatalog[1]],
            refreshInterval: TimeSpan.FromHours(3),
            timeProvider: new FixedTimeProvider(utcNow, zone));

        await engine.TryRunDueShapesAsync(utcNow.UtcDateTime);

        // 05:00 local: only the hourly shape is due.
        var hourlyOnly = await engine.TryRunDueShapesAsync(new DateTime(2026, 9, 10, 11, 0, 0, DateTimeKind.Utc));

        Assert.AreEqual(1, hourlyOnly!.Count);
        Assert.AreEqual("work_order_lookup", hourlyOnly[0].ShapeKey);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));

        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                Assert.Fail("The awaited condition was not met within the timeout.");
            }

            await Task.Delay(10);
        }
    }

    private static RefreshEngine CreateEngine(
        IVisualShapePayloadSource payloadSource,
        IMockMirrorRefreshWriter writer,
        IReadOnlyList<VisualReadShape>? catalog = null,
        TimeSpan? refreshInterval = null,
        TimeSpan? minimumScheduledWait = null,
        TimeProvider? timeProvider = null)
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
            NullLogger<RefreshEngine>.Instance,
            refreshInterval,
            minimumScheduledWait,
            timeProvider);
    }

    /// <summary>Time source with a fixed instant and an explicit local zone, so the grid is deterministic.</summary>
    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow, TimeZoneInfo localTimeZone)
        {
            _utcNow = utcNow;
            LocalZone = localTimeZone;
        }

        public TimeZoneInfo LocalZone { get; }

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public override TimeZoneInfo LocalTimeZone => LocalZone;
    }

    /// <summary>Payload source stub that returns a fixed payload, counts calls, or fails on demand.</summary>
    private sealed class StubPayloadSource : IVisualShapePayloadSource
    {
        private readonly object _sync = new();

        public Exception? ExceptionToThrow { get; init; }

        /// <summary>Per-call failure selector, used to make call 1 fail and later calls succeed.</summary>
        public Func<int, Exception?>? ExceptionForCall { get; init; }

        /// <summary>Optional gate that holds a call open, so a cycle can be observed while it runs.</summary>
        public TaskCompletionSource? Gate { get; init; }

        public string Payload { get; init; } = "[]";

        public int CallCount { get; private set; }

        public async Task<string> BuildRefreshPayloadAsync(
            VisualReadShape shape,
            CancellationToken cancellationToken = default)
        {
            int call;
            lock (_sync)
            {
                CallCount++;
                call = CallCount;
            }

            if (Gate is not null)
            {
                await Gate.Task.ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var exception = ExceptionForCall?.Invoke(call) ?? ExceptionToThrow;
            if (exception is not null)
            {
                throw exception;
            }

            return Payload;
        }
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

        /// <summary>Reports exactly the catalog's shapes, so no phantom gap is detected.</summary>
        public async Task<IReadOnlyList<VisualShapeMetadata>> GetAllShapeMetadataAsync(
            CancellationToken cancellationToken = default)
        {
            var all = new List<VisualShapeMetadata>(_shapes.Count);

            foreach (var shape in _shapes)
            {
                all.Add((await GetShapeMetadataAsync(shape.Key, cancellationToken).ConfigureAwait(false))!);
            }

            return all;
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

        /// <summary>Reports the rejected shapes themselves, so nothing is reported as unregistered.</summary>
        public Task<IReadOnlyList<VisualShapeMetadata>> GetAllShapeMetadataAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<VisualShapeMetadata>>([]);
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
