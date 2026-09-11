using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Service.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// Verifies the durable, service-local refresh run-record store: it keeps one row per shape, survives
/// a reload, degrades to empty rather than throwing on a corrupt file, and can never persist credential
/// material (FR-013, FR-026, data-model.md §4).
/// </summary>
[TestClass]
public sealed class RefreshRunRecordStoreTests
{
    private string _root = string.Empty;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "mtm-mock-run-records", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup()
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
            // A locked temp folder must not fail the test run.
        }
    }

    [TestMethod]
    public async Task RecordAsync_PersistsTheLastRunPerShape_AndReloadsItAsync()
    {
        var store = new RefreshRunRecordStore(_root);
        var startedUtc = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

        await store.RecordAsync(new RefreshRunRecord
        {
            ShapeKey = "work_order_lookup",
            StartedUtc = startedUtc,
            FinishedUtc = startedUtc.AddSeconds(2),
            Outcome = RefreshRunOutcome.Succeeded,
            RowCount = 42,
            DurationMs = 2000
        });

        await store.RecordAsync(new RefreshRunRecord
        {
            ShapeKey = "inventory_locations",
            StartedUtc = startedUtc,
            FinishedUtc = startedUtc.AddSeconds(1),
            Outcome = RefreshRunOutcome.SkippedSourceUnreachable,
            DurationMs = 5,
            ErrorMessage = "Could not connect to VISUAL."
        });

        Assert.IsTrue(File.Exists(store.FilePath), "A recorded run must be durable, not only in memory.");

        var reloaded = new RefreshRunRecordStore(_root);
        await reloaded.LoadAsync();

        var lookup = reloaded.GetLastRun("work_order_lookup");
        Assert.IsNotNull(lookup);
        Assert.AreEqual(RefreshRunOutcome.Succeeded, lookup!.Outcome);
        Assert.AreEqual(42, lookup.RowCount);

        var inventory = reloaded.GetLastRun("inventory_locations");
        Assert.IsNotNull(inventory);
        Assert.AreEqual(RefreshRunOutcome.SkippedSourceUnreachable, inventory!.Outcome);
        Assert.AreEqual(2, reloaded.GetAllLastRuns().Count);
    }

    [TestMethod]
    public async Task RecordAsync_ReplacesTheEarlierRunForTheSameShapeAsync()
    {
        var store = new RefreshRunRecordStore(_root);
        var startedUtc = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

        await store.RecordAsync(new RefreshRunRecord
        {
            ShapeKey = "work_order_lookup",
            StartedUtc = startedUtc,
            Outcome = RefreshRunOutcome.FailedUnknown,
            ErrorMessage = "first attempt failed"
        });

        await store.RecordAsync(new RefreshRunRecord
        {
            ShapeKey = "work_order_lookup",
            StartedUtc = startedUtc.AddMinutes(15),
            Outcome = RefreshRunOutcome.Succeeded,
            RowCount = 3
        });

        var last = store.GetLastRun("work_order_lookup");

        Assert.IsNotNull(last);
        Assert.AreEqual(RefreshRunOutcome.Succeeded, last!.Outcome);
        Assert.AreEqual(1, store.GetAllLastRuns().Count, "Only the most recent run per shape is retained (data-model.md §4).");
    }

    [TestMethod]
    public async Task RecordAsync_RedactsCredentialShapedErrorTextAsync()
    {
        var store = new RefreshRunRecordStore(_root);

        await store.RecordAsync(new RefreshRunRecord
        {
            ShapeKey = "work_order_lookup",
            StartedUtc = DateTime.UtcNow,
            Outcome = RefreshRunOutcome.FailedUnknown,
            ErrorMessage = "Login failed. Server=visual;User ID=shop2;Password=hunter2;Database=MTMFG;"
        });

        var persistedText = await File.ReadAllTextAsync(store.FilePath);

        Assert.IsFalse(
            persistedText.Contains("hunter2", StringComparison.Ordinal),
            "FR-026: credential material must never reach the status-backed record file.");
        Assert.IsFalse(
            store.GetLastRun("work_order_lookup")!.ErrorMessage!.Contains("hunter2", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task LoadAsync_WhenTheFileIsCorrupt_IsEmptyRatherThanThrowingAsync()
    {
        var store = new RefreshRunRecordStore(_root);
        await File.WriteAllTextAsync(store.FilePath, "{ this is not valid json");

        await store.LoadAsync();

        Assert.AreEqual(0, store.GetAllLastRuns().Count);

        // The store must still accept new runs after a corrupt file, so status keeps working.
        await store.RecordAsync(new RefreshRunRecord
        {
            ShapeKey = "work_order_lookup",
            StartedUtc = DateTime.UtcNow,
            Outcome = RefreshRunOutcome.Succeeded,
            RowCount = 1
        });

        Assert.AreEqual(1, store.GetLastRun("work_order_lookup")!.RowCount);
    }

    [TestMethod]
    public async Task LoadAsync_WhenNothingHasRunYet_IsEmptyAndDoesNotThrowAsync()
    {
        var store = new RefreshRunRecordStore(_root);

        await store.LoadAsync();

        Assert.AreEqual(0, store.GetAllLastRuns().Count);
        Assert.IsNull(store.GetLastRun("work_order_lookup"));
    }
}
