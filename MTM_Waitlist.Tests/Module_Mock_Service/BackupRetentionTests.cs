using Microsoft.Extensions.Logging.Abstractions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Service.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// Verifies the backup retention contract: the oldest artifacts beyond the store's retention are pruned,
/// a safety snapshot is never pruned or counted against the limit, and one store's retention never touches
/// another store's artifacts (FR-009, SC-008).
/// </summary>
[TestClass]
public sealed class BackupRetentionTests
{
    private static readonly DateTime BaseUtc = new(2026, 9, 10, 1, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task PruneAsync_KeepsTheNewestArtifacts_AndMarksTheRestPrunedAndDeleted()
    {
        using var fixture = new StoreFixture();
        var artifacts = await fixture.SeedAsync(BackupStore.MtmWaitlist, 5);

        var pruned = await fixture.Store.PruneAsync(BackupStore.MtmWaitlist, retentionCount: 2);

        Assert.AreEqual(3, pruned.Count, "Everything beyond the two newest artifacts must be pruned.");
        Assert.AreEqual(2, fixture.Store.GetRetainedCount(BackupStore.MtmWaitlist));

        // The newest two survive; the oldest three are marked and their files removed.
        var all = fixture.Store.GetArtifacts(BackupStore.MtmWaitlist);
        CollectionAssert.AreEqual(
            new[] { artifacts[4].ArtifactId, artifacts[3].ArtifactId },
            all.Where(artifact => artifact.IsRetained).Select(artifact => artifact.ArtifactId).ToArray());

        foreach (var artifact in all.Where(artifact => !artifact.IsRetained))
        {
            Assert.IsFalse(File.Exists(artifact.FilePath), "A pruned artifact's file must be removed.");
        }

        foreach (var artifact in all.Where(artifact => artifact.IsRetained))
        {
            Assert.IsTrue(File.Exists(artifact.FilePath), "A retained artifact's file must survive.");
        }
    }

    [TestMethod]
    public async Task PruneAsync_NeverPrunesASafetySnapshot_AndDoesNotCountItAgainstRetention()
    {
        using var fixture = new StoreFixture();
        var scheduled = await fixture.SeedAsync(BackupStore.MtmWaitlist, 4);
        var safety = await fixture.SeedAsync(BackupStore.MtmWaitlist, 1, isSafetySnapshot: true);

        var pruned = await fixture.Store.PruneAsync(BackupStore.MtmWaitlist, retentionCount: 2);

        var safetyArtifact = safety[0];
        var reloadedSafety = fixture.Store
            .GetArtifacts(BackupStore.MtmWaitlist)
            .Single(artifact => artifact.ArtifactId == safetyArtifact.ArtifactId);

        Assert.IsTrue(reloadedSafety.IsRetained, "The pre-restore safety snapshot is the recovery path and must never be pruned.");
        Assert.IsTrue(File.Exists(reloadedSafety.FilePath), "The safety snapshot's file must survive retention.");
        Assert.IsFalse(
            pruned.Any(artifact => artifact.ArtifactId == safetyArtifact.ArtifactId),
            "The safety snapshot must not appear in the pruned set.");
        Assert.AreEqual(2, fixture.Store.GetRetainedCount(BackupStore.MtmWaitlist) - 1, "Two scheduled artifacts are retained; the snapshot is extra, not counted.");

        // Exactly the two oldest scheduled artifacts go; the snapshot is excluded from the count.
        CollectionAssert.AreEquivalent(
            new[] { scheduled[0].ArtifactId, scheduled[1].ArtifactId },
            pruned.Select(artifact => artifact.ArtifactId).ToArray());
    }

    [TestMethod]
    public async Task PruneAsync_IsPerStore_OneStoresRetentionLeavesAnotherStoreIntact()
    {
        using var fixture = new StoreFixture();
        var waitlist = await fixture.SeedAsync(BackupStore.MtmWaitlist, 4);
        await fixture.SeedAsync(BackupStore.MtmWipApplicationWinforms, 4);

        var pruned = await fixture.Store.PruneAsync(BackupStore.MtmWaitlist, retentionCount: 1);

        Assert.AreEqual(3, pruned.Count);
        Assert.AreEqual(1, fixture.Store.GetRetainedCount(BackupStore.MtmWaitlist));
        Assert.AreEqual(
            4,
            fixture.Store.GetRetainedCount(BackupStore.MtmWipApplicationWinforms),
            "Pruning one store must not alter any other store's artifacts (FR-009).");

        foreach (var artifact in pruned)
        {
            Assert.IsFalse(File.Exists(artifact.FilePath));
        }

        Assert.IsTrue(
            File.Exists(waitlist[3].FilePath),
            "The one retained artifact of the pruned store must survive.");

        Assert.IsTrue(
            fixture.Store.GetArtifacts(BackupStore.MtmWipApplicationWinforms).All(artifact => File.Exists(artifact.FilePath)),
            "Another store's files must still be on disk.");
    }

    [TestMethod]
    public async Task PruneAsync_IsIdempotent_AndToleratesAMissingFile()
    {
        using var fixture = new StoreFixture();
        var artifacts = await fixture.SeedAsync(BackupStore.MtmMock, 3);

        // A file removed outside the service must not make pruning fail.
        File.Delete(artifacts[0].FilePath);

        var first = await fixture.Store.PruneAsync(BackupStore.MtmMock, retentionCount: 1);
        var second = await fixture.Store.PruneAsync(BackupStore.MtmMock, retentionCount: 1);

        Assert.AreEqual(2, first.Count);
        Assert.AreEqual(0, second.Count, "Pruning again must be a no-op rather than re-pruning.");
        Assert.AreEqual(1, fixture.Store.GetRetainedCount(BackupStore.MtmMock));
    }

    [TestMethod]
    public async Task PruneAsync_WithRetentionBelowOne_IsRejected()
    {
        using var fixture = new StoreFixture();
        await fixture.SeedAsync(BackupStore.MtmWaitlist, 1);

        await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(
            () => fixture.Store.PruneAsync(BackupStore.MtmWaitlist, retentionCount: 0));
    }

    [TestMethod]
    public async Task EnforceRetentionAsync_UsesTheRequestedRetentionForThatStoreOnly()
    {
        using var fixture = new StoreFixture();
        await fixture.SeedAsync(BackupStore.MtmReceivingApplication, 5);
        await fixture.SeedAsync(BackupStore.MtmWaitlist, 5);

        var configuration = ServiceConfiguration.CreateDefault(fixture.Root);
        var engine = new BackupEngine(
            fixture.Store,
            new MySqlConnectionStringResolver(configuration.MySqlConnection),
            () => configuration,
            NullLogger<BackupEngine>.Instance);

        await engine.EnforceRetentionAsync(BackupStore.MtmReceivingApplication, retentionCount: 2);

        Assert.AreEqual(2, fixture.Store.GetRetainedCount(BackupStore.MtmReceivingApplication));
        Assert.AreEqual(5, fixture.Store.GetRetainedCount(BackupStore.MtmWaitlist));
    }

    /// <summary>One throwaway app-data root with a loaded artifact store.</summary>
    private sealed class StoreFixture : IDisposable
    {
        private int _sequence;

        public StoreFixture()
        {
            Root = Path.Combine(Path.GetTempPath(), "mtm-backup-retention-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);

            Store = new BackupArtifactStore(Root);
        }

        public string Root { get; }

        public BackupArtifactStore Store { get; }

        /// <summary>
        /// Records artifacts with increasing creation times and real files on disk. Every artifact gets its own
        /// file: a safety snapshot seeded alongside scheduled runs must not share (and therefore lose) a path,
        /// which would make retention look like it pruned the snapshot.
        /// </summary>
        public async Task<IReadOnlyList<BackupArtifact>> SeedAsync(
            BackupStore store,
            int count,
            bool isSafetySnapshot = false)
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
                    IsSafetySnapshot = isSafetySnapshot
                };

                await Store.RecordAsync(
                    new BackupRunRecord
                    {
                        Store = store,
                        StartedUtc = timestamp,
                        FinishedUtc = timestamp,
                        Outcome = BackupRunOutcome.Succeeded,
                        ArtifactPath = filePath,
                        IsSafetySnapshot = isSafetySnapshot
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
