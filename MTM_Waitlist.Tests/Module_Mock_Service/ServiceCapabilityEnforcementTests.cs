using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Service.Api;
using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Service.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// Proof that a capability the host does not have is actually <b>not attempted</b>: refresh where Infor Visual
/// is unreachable, and one store's backup and restore where that store's database is.
/// </summary>
/// <remarks>
/// The gate is a stub here, so these tests assert the <i>enforcement</i> rather than the measurement — the
/// measurement is covered by <see cref="ServiceCapabilityGatingTests"/>. Nothing reaches the network, and a
/// machine that happens to have Visual or MySQL configured behaves identically.
/// </remarks>
[TestClass]
public sealed class ServiceCapabilityEnforcementTests
{
    [TestMethod]
    public async Task RunRefreshAsync_WhenRefreshIsDisabledOnThisHost_RefusesWithTheReasonAsync()
    {
        using var fixture = new OperationsFixture(GateWithoutRefresh());

        var outcome = await fixture.Operations.RunRefreshAsync(shapeKeys: null);

        Assert.IsFalse(outcome.Succeeded, "An on-demand refresh must be refused where refresh is disabled, not attempted.");
        Assert.AreEqual(503, outcome.StatusCode);
        Assert.AreEqual("refreshUnavailable", outcome.Error!.Error);
        StringAssert.Contains(outcome.Error.Message!, "Infor Visual");
    }

    [TestMethod]
    public async Task RunBackupAsync_WhenTheStoreCannotBeReached_RefusesWithTheReasonAsync()
    {
        using var fixture = new OperationsFixture(GateWithoutStore(BackupStore.MtmWaitlist));

        var outcome = await fixture.Operations.RunBackupAsync("mtm_waitlist");

        Assert.IsFalse(outcome.Succeeded, "A store this machine cannot reach must not be backed up.");
        Assert.AreEqual(503, outcome.StatusCode);
        Assert.AreEqual("storeUnavailable", outcome.Error!.Error);
        StringAssert.Contains(outcome.Error.Message!, "mtm_waitlist");
    }

    [TestMethod]
    public async Task RunBackupAsync_WhenOnlyAnotherStoreIsUnreachable_StillRunsThisStoreAsync()
    {
        using var fixture = new OperationsFixture(GateWithoutStore(BackupStore.MtmWaitlist));

        // One store's reachability must not disable another store's work.
        var outcome = await fixture.Operations.RunBackupAsync("mtm_mock");

        Assert.IsTrue(outcome.Succeeded, "A reachable store's backup must still run.");
        Assert.AreEqual("mtm_mock", outcome.Payload!.Store);
    }

    [TestMethod]
    public async Task GetStatusAsync_ReportsRefreshAndPerStoreCapabilityWithReasonsAsync()
    {
        using var fixture = new OperationsFixture(GateWithoutRefreshAndStore(BackupStore.MtmMock));

        var outcome = await fixture.Operations.GetStatusAsync();

        Assert.IsTrue(outcome.Succeeded);

        var payload = outcome.Payload!;
        Assert.IsFalse(payload.RefreshEnabled, "Status must say refresh is disabled on this host.");
        StringAssert.Contains(payload.RefreshDisabledReason!, "Infor Visual");

        var mock = payload.Backups.Single(backup => backup.Store == "mtm_mock");
        Assert.IsFalse(mock.IsStoreReachable, "Status must say that store cannot be reached.");
        StringAssert.Contains(mock.UnreachableReason!, "mtm_mock");

        var waitlist = payload.Backups.Single(backup => backup.Store == "mtm_waitlist");
        Assert.IsTrue(waitlist.IsStoreReachable, "A store the gate did not disable must be reported reachable.");
        Assert.IsNull(waitlist.UnreachableReason);
    }

    [TestMethod]
    public async Task GetStatusAsync_WithoutAGate_ReportsEveryCapabilityEnabledAsync()
    {
        using var fixture = new OperationsFixture(capabilityGate: null);

        var outcome = await fixture.Operations.GetStatusAsync();

        Assert.IsTrue(outcome.Payload!.RefreshEnabled, "The default must gate nothing: the engines behave as before.");
        Assert.IsTrue(outcome.Payload.Backups.All(backup => backup.IsStoreReachable));
    }

    [TestMethod]
    public async Task RunDueStoresAsync_WhenAStoreCannotBeReached_SkipsItAndStillRunsTheOthersAsync()
    {
        using var fixture = new BackupFixture(GateWithoutStore(BackupStore.MtmWipApplicationWinforms));
        var clock = TimeProvider.System;

        // The first pass seeds each store's next due time; the second is far past every slot in the day.
        await fixture.Scheduler.RunDueStoresAsync(clock.GetUtcNow().UtcDateTime, clock);
        var attempted = await fixture.Scheduler.RunDueStoresAsync(clock.GetUtcNow().UtcDateTime.AddDays(2), clock);

        Assert.IsFalse(
            attempted.Contains(BackupStore.MtmWipApplicationWinforms),
            "A store this machine cannot reach must not be attempted by the schedule.");
        Assert.IsTrue(attempted.Contains(BackupStore.MtmWaitlist), "Every other store must still run on its slot.");
        Assert.IsTrue(
            fixture.Artifacts.GetLastRun(BackupStore.MtmWipApplicationWinforms) is null,
            "A skipped store must not record a run, so its last real backup stays visible.");
    }

    [TestMethod]
    public async Task ConfirmAndRestoreAsync_WhenTheStoreCannotBeReached_RefusesBeforeTakingASafetySnapshotAsync()
    {
        using var fixture = new RestoreFixture(GateWithoutStore(BackupStore.MtmWaitlist));

        var artifact = fixture.CreateArtifact(BackupStore.MtmWaitlist);
        var request = fixture.Restore.RequestRestore(artifact);

        var outcome = await fixture.Restore.ConfirmAndRestoreAsync(request, artifact);

        Assert.AreEqual(RestoreOutcomeKind.StoreUnavailable, outcome.Outcome, "Restore must be disabled for that store, not attempted.");
        Assert.IsNull(outcome.SafetySnapshotArtifactId, "Nothing may be taken or changed for a restore that will not run.");
        StringAssert.Contains(outcome.VerificationSummary!, "mtm_waitlist");
        Assert.IsNull(fixture.Artifacts.GetLastRun(BackupStore.MtmWaitlist), "A refused restore must not run a backup.");
    }

    private static ServiceCapabilitySnapshot GateWithoutRefresh() =>
        new(
            VisualCapabilityState.Unreachable("Infor Visual did not accept a connection from this machine, so refresh is disabled here."),
            BackupStoreExtensions.All.ToDictionary(store => store, StoreCapabilityState.Available),
            DateTimeOffset.UtcNow);

    private static ServiceCapabilitySnapshot GateWithoutStore(BackupStore store) =>
        new(
            VisualCapabilityState.Available,
            BackupStoreExtensions.All.ToDictionary(
                candidate => candidate,
                candidate => candidate == store
                    ? StoreCapabilityState.Unreachable(candidate, $"MySQL did not accept a connection to '{candidate.ToDatabaseName()}' from this machine.")
                    : StoreCapabilityState.Available(candidate)),
            DateTimeOffset.UtcNow);

    private static ServiceCapabilitySnapshot GateWithoutRefreshAndStore(BackupStore store)
    {
        var withoutRefresh = GateWithoutRefresh();

        return withoutRefresh with
        {
            Stores = BackupStoreExtensions.All.ToDictionary(
                candidate => candidate,
                candidate => candidate == store
                    ? StoreCapabilityState.Unreachable(candidate, $"MySQL did not accept a connection to '{candidate.ToDatabaseName()}' from this machine.")
                    : StoreCapabilityState.Available(candidate))
        };
    }

    /// <summary>The gate as a stub, so the tests assert enforcement and never measurement.</summary>
    private sealed class StubCapabilityGate : IServiceCapabilityGate
    {
        private readonly ServiceCapabilitySnapshot _snapshot;

        public StubCapabilityGate(ServiceCapabilitySnapshot snapshot) => _snapshot = snapshot;

        public ServiceCapabilitySnapshot Current => _snapshot;

        public Task<ServiceCapabilitySnapshot> GetCapabilitiesAsync(
            bool reprobe = false,
            CancellationToken cancellationToken = default) => Task.FromResult(_snapshot);
    }

    /// <summary>A throwaway app-data root, cleaned up after the test.</summary>
    private abstract class TempRootFixture : IDisposable
    {
        protected TempRootFixture()
        {
            Root = Path.Combine(Path.GetTempPath(), "mtm-mock-capability-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, recursive: true);
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

    /// <summary>Builds the API operations facade over real stores and a stubbed gate.</summary>
    private sealed class OperationsFixture : TempRootFixture
    {
        public OperationsFixture(ServiceCapabilitySnapshot? capabilityGate)
            : this(capabilityGate is null ? null : new StubCapabilityGate(capabilityGate))
        {
        }

        private OperationsFixture(IServiceCapabilityGate? capabilityGate)
        {
            ConfigurationStore = new ServiceConfigurationStore(Root);
            ConfigurationStore.LoadAsync().GetAwaiter().GetResult();

            var configuration = ConfigurationStore.Current;
            var catalogProvider = new RefreshShapeCatalogProvider(
                new AcceptingMetadataReader(),
                catalog: null,
                contentRoot: AppContext.BaseDirectory);
            catalogProvider.ValidateAsync().GetAwaiter().GetResult();

            Artifacts = new BackupArtifactStore(Root);

            var refreshEngine = new RefreshEngine(
                catalogProvider,
                new NoOpPayloadSource(),
                new NoOpMirrorWriter(),
                NullLogger<RefreshEngine>.Instance,
                capabilityGate: capabilityGate);

            var backupEngine = new BackupEngine(
                Artifacts,
                new RunHistoryStore(Root, NullLogger<RunHistoryStore>.Instance),
                new MySqlConnectionStringResolver(configuration.MySqlConnection),
                () => ConfigurationStore.Current,
                NullLogger<BackupEngine>.Instance);

            Operations = new ServiceApiOperations(
                refreshEngine,
                catalogProvider,
                new RefreshRunRecordStore(Root),
                backupEngine,
                Artifacts,
                new EmptyFreshnessReader(),
                new NullConnectivityProbe(),
                ConfigurationStore,
                capabilityGate: capabilityGate);
        }

        public ServiceConfigurationStore ConfigurationStore { get; }

        public BackupArtifactStore Artifacts { get; }

        public ServiceApiOperations Operations { get; }
    }

    /// <summary>Builds a real scheduler and engine over a throwaway root.</summary>
    private sealed class BackupFixture : TempRootFixture
    {
        public BackupFixture(ServiceCapabilitySnapshot gate)
        {
            Configuration = ServiceConfiguration.CreateDefault(Root);
            Artifacts = new BackupArtifactStore(Root);

            var engine = new BackupEngine(
                Artifacts,
                new RunHistoryStore(Root, NullLogger<RunHistoryStore>.Instance),
                new MySqlConnectionStringResolver(Configuration.MySqlConnection),
                () => Configuration,
                NullLogger<BackupEngine>.Instance);

            Scheduler = new BackupScheduler(
                engine,
                () => Configuration,
                NullLogger<BackupScheduler>.Instance,
                minimumWait: TimeSpan.FromMilliseconds(1),
                capabilityGate: new StubCapabilityGate(gate));
        }

        public BackupArtifactStore Artifacts { get; }

        public BackupScheduler Scheduler { get; }

        private ServiceConfiguration Configuration { get; }
    }

    /// <summary>Builds a real restore service, with one artifact on disk.</summary>
    private sealed class RestoreFixture : TempRootFixture
    {
        public RestoreFixture(ServiceCapabilitySnapshot gate)
        {
            Configuration = ServiceConfiguration.CreateDefault(Root);
            Artifacts = new BackupArtifactStore(Root);

            var engine = new BackupEngine(
                Artifacts,
                new RunHistoryStore(Root, NullLogger<RunHistoryStore>.Instance),
                new MySqlConnectionStringResolver(Configuration.MySqlConnection),
                () => Configuration,
                NullLogger<BackupEngine>.Instance);

            Restore = new RestoreService(
                engine,
                Artifacts,
                new MySqlConnectionStringResolver(Configuration.MySqlConnection),
                () => Configuration,
                NullLogger<RestoreService>.Instance,
                capabilityGate: new StubCapabilityGate(gate));
        }

        public BackupArtifactStore Artifacts { get; }

        public RestoreService Restore { get; }

        private ServiceConfiguration Configuration { get; }

        /// <summary>Writes a dump-shaped file, because a restore refuses an artifact that is not on disk.</summary>
        /// <param name="store">The store the artifact belongs to.</param>
        public BackupArtifact CreateArtifact(BackupStore store)
        {
            var path = Path.Combine(Root, $"{store.ToDatabaseName()}-{Guid.NewGuid():N}.sql");
            File.WriteAllText(path, "-- test artifact");

            return new BackupArtifact
            {
                Store = store,
                CreatedUtc = DateTime.UtcNow,
                FilePath = path,
                SizeBytes = new FileInfo(path).Length
            };
        }
    }

    private sealed class NoOpPayloadSource : IVisualShapePayloadSource
    {
        public Task<string> BuildRefreshPayloadAsync(VisualReadShape shape, CancellationToken cancellationToken = default) =>
            Task.FromResult("[]");
    }

    private sealed class NoOpMirrorWriter : IMockMirrorRefreshWriter
    {
        public Task<int> RefreshAsync(VisualReadShape shape, string jsonPayload, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    private sealed class EmptyFreshnessReader : IVisualShapeFreshnessReader
    {
        public Task<IReadOnlyList<VisualShapeFreshness>> GetFreshnessAsync(
            string? shapeKey = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<VisualShapeFreshness>>([]);
    }

    private sealed class NullConnectivityProbe : IVisualConnectivityProbe
    {
        public Task<bool> ProbeAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
    }
}
