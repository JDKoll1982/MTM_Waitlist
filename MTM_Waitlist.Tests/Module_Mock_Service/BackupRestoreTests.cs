using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Service.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// FR-009/FR-010/FR-013 and FR-023's backup/restore gate: each store's backups are its own, a missing tool is
/// reported without inventing an artifact, an unconfirmed restore changes nothing, and a restore that cannot
/// take its safety snapshot refuses rather than destroying the store.
/// </summary>
/// <remarks>
/// <para>
/// <b>What is asserted here, and what is not.</b> These tests drive the real <see cref="BackupEngine"/> and
/// <see cref="RestoreService"/> with the MySQL tools deliberately absent, which is the state that matters most:
/// it is the state in which the service must refuse to act rather than act badly. The happy path — a confirmed
/// restore that really replaces and verifies a store — needs a live MySQL server and is covered by the
/// environment-gated live-database test (T118), exactly as the refresh swap is.
/// </para>
/// <para>
/// <b>No code path from the API to a restore</b> is asserted separately, by reflection, in
/// <c>ServiceApiSecurityTests.NoCodePathLeadsFromTheApiToARestore</c>, and at the listener level by
/// <c>ServiceApiTests.Restore_IsNotReachableOverTheNetwork_EvenWithAValidCredential</c>.
/// </para>
/// </remarks>
[TestClass]
public sealed class BackupRestoreTests
{
    private static readonly DateTime BaseUtc = new(2026, 9, 11, 8, 0, 0, DateTimeKind.Utc);

    private string? _originalPath;

    /// <summary>
    /// <see cref="BackupEngine.ResolveToolPath"/> falls back to searching <c>PATH</c> for <c>mysqldump.exe</c>,
    /// so a developer machine that happens to have the MySQL client tools installed would silently exercise a
    /// different path than the one under test. <c>PATH</c> is cleared for the duration of each test and restored
    /// afterwards; the suite runs sequentially (no assembly-level <c>Parallelize</c>), so nothing else is affected.
    /// </summary>
    [TestInitialize]
    public void HideAnyInstalledMySqlClientTools()
    {
        _originalPath = Environment.GetEnvironmentVariable("PATH");
        Environment.SetEnvironmentVariable("PATH", string.Empty);
    }

    [TestCleanup]
    public void RestorePath()
    {
        Environment.SetEnvironmentVariable("PATH", _originalPath);
    }

    [TestMethod]
    public async Task RunAsync_WhenTheToolIsMissing_ReportsToolUnavailableAndRecordsNoArtifact()
    {
        using var fixture = new StoreFixture();
        var engine = fixture.CreateEngine(mysqldumpPath: Path.Combine(fixture.Root, "no-such-tool.exe"));

        var run = await engine.RunAsync(BackupStore.MtmWaitlist);

        Assert.AreEqual(BackupRunOutcome.ToolUnavailable, run.Outcome);
        Assert.IsNull(run.ArtifactPath, "A missing tool produces no artifact path.");
        Assert.AreEqual(
            0,
            fixture.Store.GetArtifacts(BackupStore.MtmWaitlist).Count,
            "A missing tool must record no artifact — a partial or zero-length dump is never a success (FR-013).");
    }

    [TestMethod]
    public async Task RunAsync_ForOneStore_LeavesEveryOtherStoreUntouched()
    {
        using var fixture = new StoreFixture();
        var otherStoreArtifacts = await fixture.SeedAsync(BackupStore.MtmReceivingApplication, 2);
        var engine = fixture.CreateEngine(mysqldumpPath: Path.Combine(fixture.Root, "no-such-tool.exe"));

        _ = await engine.RunAsync(BackupStore.MtmWaitlist);

        var other = fixture.Store.GetArtifacts(BackupStore.MtmReceivingApplication);
        Assert.AreEqual(2, other.Count, "Disabling or failing one store must not change another store's artifacts.");
        Assert.IsTrue(
            otherStoreArtifacts.All(seeded => other.Any(candidate => candidate.FilePath == seeded.FilePath)),
            "The other store's artifacts must be exactly the ones it had before this run.");
        Assert.IsTrue(
            other.All(artifact => File.Exists(artifact.FilePath)),
            "Another store's files must still be on disk.");
    }

    [TestMethod]
    public async Task RequestRestore_RecordsTheIntentWithoutChangingAnything()
    {
        using var fixture = new StoreFixture();
        var artifacts = await fixture.SeedAsync(BackupStore.MtmWaitlist, 1);
        var restore = fixture.CreateRestoreService();
        var before = Directory.GetFiles(fixture.Root, "*", SearchOption.AllDirectories).Length;

        var request = restore.RequestRestore(artifacts[0]);

        Assert.AreEqual(RestoreOutcomeKind.NotConfirmed, request.Outcome);
        Assert.AreEqual(artifacts[0].ArtifactId, request.ArtifactId);
        Assert.AreEqual(BackupStore.MtmWaitlist, request.Store);
        Assert.IsNull(request.ConfirmedUtc, "An unconfirmed request has not been confirmed.");
        Assert.AreEqual(
            1,
            fixture.Store.GetArtifacts(BackupStore.MtmWaitlist).Count,
            "Requesting a restore must not record a run or an artifact.");
        Assert.AreEqual(
            before,
            Directory.GetFiles(fixture.Root, "*", SearchOption.AllDirectories).Length,
            "Requesting a restore must not write anything to disk.");
    }

    [TestMethod]
    public async Task ConfirmAndRestoreAsync_WhenTheSafetySnapshotCannotBeTaken_RefusesAndChangesNothing()
    {
        using var fixture = new StoreFixture();
        var artifacts = await fixture.SeedAsync(BackupStore.MtmWaitlist, 1);
        var restore = fixture.CreateRestoreService();
        var request = restore.RequestRestore(artifacts[0]);

        var outcome = await restore.ConfirmAndRestoreAsync(request, artifacts[0]);

        Assert.AreEqual(
            RestoreOutcomeKind.FailedReload,
            outcome.Outcome,
            "Without a recovery point the restore must abort rather than drop the store (FR-010).");
        StringAssert.Contains(outcome.VerificationSummary, "nothing was changed");
        Assert.IsNull(
            outcome.SafetySnapshotArtifactId,
            "No safety snapshot exists, so none may be named as a recovery path.");
        Assert.AreEqual(
            1,
            fixture.Store.GetArtifacts(BackupStore.MtmWaitlist).Count,
            "The aborted restore must not record a new artifact.");
        Assert.IsTrue(
            File.Exists(artifacts[0].FilePath),
            "The artifact the operator selected must still be there.");
    }

    [TestMethod]
    public async Task ConfirmAndRestoreAsync_WithAnArtifactThatWasNotRequested_IsRejected()
    {
        using var fixture = new StoreFixture();
        var artifacts = await fixture.SeedAsync(BackupStore.MtmWaitlist, 2);
        var restore = fixture.CreateRestoreService();
        var request = restore.RequestRestore(artifacts[0]);

        // Confirming with a different artifact is the mistake this guard exists to stop.
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => restore.ConfirmAndRestoreAsync(request, artifacts[1]));
    }

    [TestMethod]
    public async Task ConfirmAndRestoreAsync_WhenTheArtifactIsGone_IsRejected()
    {
        using var fixture = new StoreFixture();
        var artifacts = await fixture.SeedAsync(BackupStore.MtmWaitlist, 1);
        var restore = fixture.CreateRestoreService();
        var request = restore.RequestRestore(artifacts[0]);
        File.Delete(artifacts[0].FilePath);

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => restore.ConfirmAndRestoreAsync(request, artifacts[0]));
    }

    /// <summary>
    /// One throwaway app-data root, with the MySQL tools deliberately absent and a configuration that points
    /// at a tool path the fixture controls.
    /// </summary>
    private sealed class StoreFixture : IDisposable
    {
        private int _sequence;

        public StoreFixture()
        {
            Root = Path.Combine(Path.GetTempPath(), "mtm-backup-restore-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);

            Store = new BackupArtifactStore(Root);
        }

        public string Root { get; }

        public BackupArtifactStore Store { get; }

        public BackupEngine CreateEngine(string mysqldumpPath)
        {
            var configuration = ServiceConfiguration.CreateDefault(Root) with { MysqldumpPath = mysqldumpPath };

            return new BackupEngine(
                Store,
                new MySqlConnectionStringResolver(configuration.MySqlConnection),
                () => configuration,
                NullLogger<BackupEngine>.Instance);
        }

        public RestoreService CreateRestoreService()
        {
            var engine = CreateEngine(Path.Combine(Root, "no-such-tool.exe"));
            var configuration = ServiceConfiguration.CreateDefault(Root);

            return new RestoreService(
                engine,
                Store,
                () => configuration,
                NullLogger<RestoreService>.Instance);
        }

        /// <summary>Records artifacts with increasing creation times and real files on disk.</summary>
        public async Task<IReadOnlyList<BackupArtifact>> SeedAsync(BackupStore store, int count)
        {
            var created = new List<BackupArtifact>(count);

            for (var index = 0; index < count; index++)
            {
                var timestamp = BaseUtc.AddMinutes(index);
                var directory = Path.Combine(Root, "artifacts", store.ToDatabaseName());
                Directory.CreateDirectory(directory);

                var filePath = Path.Combine(
                    directory,
                    $"{store.ToDatabaseName()}_{timestamp:yyyyMMdd'T'HHmmss'Z'}_{_sequence++:D3}.sql");
                await File.WriteAllTextAsync(filePath, "-- dump");

                var artifact = new BackupArtifact
                {
                    Store = store,
                    CreatedUtc = timestamp,
                    FilePath = filePath,
                    SizeBytes = new FileInfo(filePath).Length,
                    IsRetained = true,
                    IsSafetySnapshot = false,
                };

                await Store.RecordAsync(
                    new BackupRunRecord
                    {
                        Store = store,
                        StartedUtc = timestamp,
                        FinishedUtc = timestamp,
                        Outcome = BackupRunOutcome.Succeeded,
                        ArtifactPath = filePath,
                        IsSafetySnapshot = false,
                    },
                    artifact);

                created.Add(artifact);
            }

            return created;
        }

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
}
