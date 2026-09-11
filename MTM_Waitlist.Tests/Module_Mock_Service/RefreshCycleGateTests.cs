using Microsoft.Extensions.Logging.Abstractions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Service.Services;
using MTM_Waitlist.Mock.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// Verifies the two behaviours that make the refresh pipeline's guarantees real rather than nominal:
/// exactly one cycle runs at a time on <b>every</b> path (FR-008), and completed runs are actually persisted
/// so per-item last-refresh status can be reported (FR-013).
/// </summary>
[TestClass]
public sealed class RefreshCycleGateTests
{
    [TestMethod]
    public async Task TryRunShapesAsync_WhileACycleIsRunning_RefusesTheSecondCallInsteadOfOverlapping()
    {
        using var fixture = new EngineFixture();
        var shape = fixture.RefreshableShapes[0];

        var firstCycle = fixture.Engine.TryRunShapesAsync([shape]);
        await fixture.PayloadSource.WaitUntilEnteredAsync();

        var secondCycle = await fixture.Engine.TryRunShapesAsync([shape]);

        Assert.IsNull(
            secondCycle,
            "A second on-demand cycle must be refused, not overlapped: two writers on the same stage twin and RENAME swap is what SC-005 forbids.");

        fixture.PayloadSource.Release();
        var firstRecords = await firstCycle;

        Assert.IsNotNull(firstRecords);
        Assert.AreEqual(1, firstRecords!.Count);
    }

    [TestMethod]
    public async Task TryRunShapesAsync_OnceTheCycleFinished_RunsAgain()
    {
        using var fixture = new EngineFixture();
        var shape = fixture.RefreshableShapes[0];

        fixture.PayloadSource.Release();

        var first = await fixture.Engine.TryRunShapesAsync([shape]);
        var second = await fixture.Engine.TryRunShapesAsync([shape]);

        Assert.IsNotNull(first);
        Assert.IsNotNull(second, "The gate must be released when a cycle ends.");
    }

    [TestMethod]
    public async Task TryRunShapesAsync_OnlyRunsTheRequestedShapes()
    {
        using var fixture = new EngineFixture();

        Assert.IsTrue(fixture.RefreshableShapes.Count >= 2, "This test needs at least two refreshable shapes.");

        fixture.PayloadSource.Release();

        var records = await fixture.Engine.TryRunShapesAsync([fixture.RefreshableShapes[1]]);

        Assert.IsNotNull(records);
        Assert.AreEqual(1, records!.Count);
        Assert.AreEqual(fixture.RefreshableShapes[1].Key, records[0].ShapeKey);
    }

    [TestMethod]
    public async Task Recorder_PersistsEveryCompletedRun_SoLastRunStatusIsReal()
    {
        using var fixture = new EngineFixture();
        var shape = fixture.RefreshableShapes[0];

        using var recorder = new RefreshRunRecordRecorder(
            fixture.Engine,
            fixture.RunRecordStore,
            NullLogger<RefreshRunRecordRecorder>.Instance);

        fixture.PayloadSource.Release();

        var records = await fixture.Engine.TryRunShapesAsync([shape]);
        Assert.IsNotNull(records);

        // Wait on the recorder's own completion, not on the store: the store's in-memory record is set inside
        // RecordAsync, so observing it alone can race the recorder's continuation.
        await WaitUntilAsync(() => recorder.RecordedCount >= 1);

        var lastRun = fixture.RunRecordStore.GetLastRun(shape.Key);

        Assert.IsNotNull(lastRun, "Without this subscription the store is read but never written, and every shape reports \"Never\" (FR-013).");
        Assert.AreEqual(shape.Key, lastRun!.ShapeKey);
        Assert.AreEqual(RefreshRunOutcome.Succeeded, lastRun.Outcome);
        Assert.AreEqual(1, recorder.RecordedCount);
    }

    [TestMethod]
    public async Task Recorder_StopsRecordingOnceDisposed()
    {
        using var fixture = new EngineFixture();
        var shape = fixture.RefreshableShapes[0];

        var recorder = new RefreshRunRecordRecorder(
            fixture.Engine,
            fixture.RunRecordStore,
            NullLogger<RefreshRunRecordRecorder>.Instance);

        recorder.Dispose();
        fixture.PayloadSource.Release();

        await fixture.Engine.TryRunShapesAsync([shape]);
        await Task.Delay(50);

        Assert.IsNull(fixture.RunRecordStore.GetLastRun(shape.Key));
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 2000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;

        while (Environment.TickCount64 < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }
    }

    /// <summary>A validated catalog, a blocking payload source, and a real run-record store in a temp folder.</summary>
    private sealed class EngineFixture : IDisposable
    {
        private readonly string _root;

        public EngineFixture()
        {
            _root = Path.Combine(Path.GetTempPath(), "mtm-refresh-gate-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);

            var catalogProvider = new RefreshShapeCatalogProvider(
                new AcceptingMetadataReader(),
                catalog: null,
                contentRoot: AppContext.BaseDirectory);

            catalogProvider.ValidateAsync().GetAwaiter().GetResult();
            CatalogProvider = catalogProvider;

            PayloadSource = new BlockingPayloadSource();

            Engine = new RefreshEngine(
                catalogProvider,
                PayloadSource,
                new NoOpMirrorWriter(),
                NullLogger<RefreshEngine>.Instance,
                refreshInterval: TimeSpan.FromHours(3),
                minimumScheduledWait: TimeSpan.FromMilliseconds(20));

            RunRecordStore = new RefreshRunRecordStore(_root);
            RunRecordStore.LoadAsync().GetAwaiter().GetResult();
        }

        public RefreshShapeCatalogProvider CatalogProvider { get; }

        public IReadOnlyList<VisualReadShape> RefreshableShapes => CatalogProvider.RefreshableShapes;

        public BlockingPayloadSource PayloadSource { get; }

        public RefreshEngine Engine { get; }

        public RefreshRunRecordStore RunRecordStore { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_root))
                {
                    Directory.Delete(_root, recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    /// <summary>Holds a cycle open until released, so a second request can be attempted against a busy engine.</summary>
    private sealed class BlockingPayloadSource : IVisualShapePayloadSource
    {
        private readonly SemaphoreSlim _entered = new(0);
        private readonly SemaphoreSlim _release = new(0);

        public async Task<string> BuildRefreshPayloadAsync(
            VisualReadShape shape,
            CancellationToken cancellationToken = default)
        {
            _entered.Release();
            await _release.WaitAsync(cancellationToken).ConfigureAwait(false);
            return "[]";
        }

        public Task WaitUntilEnteredAsync() => _entered.WaitAsync(TimeSpan.FromSeconds(5));

        public void Release()
        {
            while (_release.CurrentCount < 8)
            {
                _release.Release();
            }
        }
    }

    /// <summary>Records nothing; these tests never touch a database.</summary>
    private sealed class NoOpMirrorWriter : IMockMirrorRefreshWriter
    {
        public Task<int> RefreshAsync(
            VisualReadShape shape,
            string jsonPayload,
            CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
