using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Service.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// Verifies the service's append-only run history — the file SC-007 and SC-008 are measured against (T230).
/// </summary>
/// <remarks>
/// <para>
/// The assertions that matter most are the ones about the file's <b>shape</b>, not about the store's API. The
/// history exists to be read by <c>scripts/service/measure-reliability-window.ps1</c>, in another language that cannot
/// reference these types, so a renamed property or a reworded outcome silently turns a measured criterion back
/// into an unmeasured one. These tests pin the contract.
/// </para>
/// <para>
/// <b>No instant in this class is a literal.</b> The store drops anything older than
/// <see cref="RunHistoryStore.RetentionDays"/> against the clock it is handed, so a test that wrote a fixed
/// calendar date would pass only until that date fell out of the window, then fail with an empty file. Every
/// instant here is derived from <see cref="_nowUtc"/>, which is this test's own clock <i>and</i> the clock the
/// store is built with, so what a test writes is always inside the window by construction.
/// </para>
/// </remarks>
[TestClass]
public sealed class RunHistoryStoreTests
{
    /// <summary>The credential-shaped text the redaction test plants, so no real secret is ever in a test.</summary>
    private const string SecretPlaceholder = "placeholder-not-a-secret";

    /// <summary>The plant host the shared connection string names, the other half of the planted leak.</summary>
    private const string PlantHost = "172.16.1.104";

    private string _root = string.Empty;

    /// <summary>This test's clock, and the clock its store is built with.</summary>
    private DateTime _nowUtc;

    /// <summary>The cycle instant a refresh is recorded against: inside the window, never a literal.</summary>
    private DateTime _cycleUtc;

    /// <summary>The window instant a backup is recorded against: inside the window, never a literal.</summary>
    private DateTime _windowUtc;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "mtm-run-history-tests", Guid.NewGuid().ToString("N"));

        // Whole seconds, because the file round-trips through ISO 8601 and every assertion here compares an
        // instant against what was read back.
        _nowUtc = TruncateToSecond(DateTime.UtcNow);
        _cycleUtc = _nowUtc.AddHours(-1);
        _windowUtc = _nowUtc.AddHours(-2);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [TestMethod]
    public void Service_RunHistoryStoreTests_FileName_IsTheOneTheMeasurementToolLooksFor()
    {
        Assert.AreEqual("run-history.jsonl", RunHistoryStore.FileName);
        Assert.AreEqual(RunHistoryStore.FileName, CreateStore().FilePath.Split(Path.DirectorySeparatorChar)[^1]);
    }

    [TestMethod]
    public async Task Service_RunHistoryStoreTests_AppendRefreshAsync_WritesThePropertyNamesTheMeasurementToolReads()
    {
        var store = CreateStore();

        await store.AppendRefreshAsync(SucceededRefresh("work_order_lookup", _cycleUtc, rows: 701));

        var line = (await ReadLinesAsync(store)).Single();
        using var document = JsonDocument.Parse(line);
        var root = document.RootElement;

        Assert.AreEqual("refresh", root.GetProperty("record").GetString());
        Assert.AreEqual(Written(_cycleUtc), root.GetProperty("cycleUtc").GetString());
        Assert.AreEqual("work_order_lookup", root.GetProperty("shape").GetString());
        Assert.AreEqual("Succeeded", root.GetProperty("outcome").GetString());
        Assert.AreEqual(701, root.GetProperty("rows").GetInt32());
        Assert.AreEqual("scheduled", root.GetProperty("trigger").GetString());
    }

    [TestMethod]
    public async Task Service_RunHistoryStoreTests_AppendRefreshAsync_OmitsTheMembersThatDoNotApplyToARefresh()
    {
        var store = CreateStore();

        await store.AppendRefreshAsync(SucceededRefresh("operation_sequences", _cycleUtc));

        using var document = JsonDocument.Parse((await ReadLinesAsync(store)).Single());
        var root = document.RootElement;

        Assert.IsFalse(root.TryGetProperty("store", out _), "a refresh line is not a backup line");
        Assert.IsFalse(root.TryGetProperty("windowUtc", out _));
        Assert.IsFalse(root.TryGetProperty("artifactPath", out _));
        Assert.IsFalse(root.TryGetProperty("reason", out _), "a success has nothing to explain");
    }

    [TestMethod]
    [DataRow(RefreshRunOutcome.Succeeded, "Succeeded", null)]
    [DataRow(RefreshRunOutcome.SkippedSourceUnreachable, "Skipped", "sourceUnreachable")]
    [DataRow(RefreshRunOutcome.FailedSchemaMismatch, "Failed", "schemaMismatch")]
    [DataRow(RefreshRunOutcome.FailedLoad, "Failed", "loadFailed")]
    [DataRow(RefreshRunOutcome.FailedUnknown, "Failed", "unexpectedError")]
    [DataRow(RefreshRunOutcome.Pending, "Pending", null)]
    public async Task Service_RunHistoryStoreTests_AppendRefreshAsync_MapsEachOutcomeOntoTheVocabularyTheCriteriaAreWrittenIn(
        RefreshRunOutcome outcome,
        string expectedOutcome,
        string? expectedReason)
    {
        var store = CreateStore();
        var record = SucceededRefresh("subordinate_parts", _cycleUtc) with { Outcome = outcome };

        await store.AppendRefreshAsync(record);

        var entry = (await store.ReadAsync()).Single();
        Assert.AreEqual(expectedOutcome, entry.Outcome);
        Assert.AreEqual(expectedReason, entry.Reason);
    }

    [TestMethod]
    public async Task Service_RunHistoryStoreTests_AppendRefreshAsync_OnlyClaimsRowsForASuccess()
    {
        var store = CreateStore();

        await store.AppendRefreshAsync(SucceededRefresh("inventory_locations", _cycleUtc, rows: 42));
        await store.AppendRefreshAsync(
            SucceededRefresh("inventory_locations", _cycleUtc) with
            {
                Outcome = RefreshRunOutcome.FailedLoad,
                RowCount = 42
            });

        var entries = await store.ReadAsync();
        Assert.AreEqual(42, entries[0].Rows);
        Assert.IsNull(entries[1].Rows, "a failed run's row count is not a reading of anything");
    }

    [TestMethod]
    public async Task Service_RunHistoryStoreTests_AppendRefreshAsync_GivesEveryShapeOfOneCycleTheSameCycleInstant()
    {
        var store = CreateStore();

        foreach (var shape in new[] { "work_order_lookup", "operation_sequences", "subordinate_parts" })
        {
            await store.AppendRefreshAsync(SucceededRefresh(shape, _cycleUtc));
        }

        var entries = await store.ReadAsync();
        Assert.AreEqual(3, entries.Count);
        Assert.AreEqual(1, entries.Select(e => e.CycleUtc).Distinct().Count(), "one cycle must group as one cycle");
        Assert.AreEqual(_cycleUtc, entries[0].CycleUtc);
    }

    [TestMethod]
    public async Task Service_RunHistoryStoreTests_AppendRefreshAsync_KeepsAnOnDemandRunOutOfTheScheduledReading()
    {
        var store = CreateStore();

        await store.AppendRefreshAsync(SucceededRefresh("disposition_input", _cycleUtc));
        await store.AppendRefreshAsync(
            SucceededRefresh("disposition_input", _cycleUtc) with { Trigger = RefreshRunTrigger.OnDemand });

        var entries = await store.ReadAsync();
        Assert.AreEqual(RunHistoryStore.ScheduledTrigger, entries[0].Trigger);
        Assert.AreEqual(RunHistoryStore.OnDemandTrigger, entries[1].Trigger);
    }

    [TestMethod]
    public async Task Service_RunHistoryStoreTests_AppendRefreshAsync_AppendsRatherThanReplacingThePreviousRun()
    {
        var store = CreateStore();

        for (var i = 0; i < 3; i++)
        {
            await store.AppendRefreshAsync(SucceededRefresh("work_order_lookup", _cycleUtc, rows: i));
        }

        Assert.AreEqual(3, (await ReadLinesAsync(store)).Count);
    }

    [TestMethod]
    public async Task Service_RunHistoryStoreTests_AppendRefreshAsync_WhenTheInstantIsLocalWritesTheUtcDesignator()
    {
        var store = CreateStore();
        var local = _cycleUtc.ToLocalTime();

        await store.AppendRefreshAsync(SucceededRefresh("work_order_lookup", local));

        var line = (await ReadLinesAsync(store)).Single();
        Assert.IsTrue(line.Contains("\"cycleUtc\":\"", StringComparison.Ordinal), line);
        Assert.IsTrue(line.Contains("Z\"", StringComparison.Ordinal), $"the instant must be marked UTC, or a reader shifts it: {line}");

        var entry = (await store.ReadAsync()).Single();
        Assert.AreEqual(DateTimeKind.Utc, entry.CycleUtc.Kind);
        Assert.AreEqual(local.ToUniversalTime(), entry.CycleUtc);
    }

    [TestMethod]
    public async Task Service_RunHistoryStoreTests_AppendRefreshAsync_WhenTheInstantCarriesNoKindTreatsItAsUtc()
    {
        var store = CreateStore();
        var unspecified = DateTime.SpecifyKind(_cycleUtc, DateTimeKind.Unspecified);

        await store.AppendRefreshAsync(SucceededRefresh("work_order_lookup", unspecified));

        var entry = (await store.ReadAsync()).Single();
        Assert.AreEqual(unspecified, entry.CycleUtc, "an unmarked instant is UTC, not a silent local-time shift");
    }

    [TestMethod]
    public async Task Service_RunHistoryStoreTests_AppendRefreshAsync_RedactsCredentialShapedTextFromTheDetail()
    {
        var store = CreateStore();
        var record = SucceededRefresh("work_order_lookup", _cycleUtc) with
        {
            Outcome = RefreshRunOutcome.FailedLoad,
            ErrorMessage = $"connect failed: Password={SecretPlaceholder};Server={PlantHost}"
        };

        await store.AppendRefreshAsync(record);

        var line = (await ReadLinesAsync(store)).Single();
        Assert.IsFalse(
            line.Contains(SecretPlaceholder, StringComparison.Ordinal),
            $"a credential must never reach the history: {line}");
    }

    [TestMethod]
    public async Task Service_RunHistoryStoreTests_AppendBackupAsync_WritesTheArtifactOfASuccessfulRun()
    {
        var store = CreateStore();
        var artifactPath = ArtifactPathFor("mtm_waitlist", _windowUtc);

        await store.AppendBackupAsync(
            SucceededBackup(BackupStore.MtmWaitlist, _windowUtc),
            new BackupArtifact
            {
                Store = BackupStore.MtmWaitlist,
                CreatedUtc = _windowUtc,
                FilePath = artifactPath,
                SizeBytes = 184320
            });

        var line = (await ReadLinesAsync(store)).Single();
        using var document = JsonDocument.Parse(line);
        var root = document.RootElement;

        Assert.AreEqual("backup", root.GetProperty("record").GetString());
        Assert.AreEqual(Written(_windowUtc), root.GetProperty("windowUtc").GetString());
        Assert.AreEqual("mtm_waitlist", root.GetProperty("store").GetString());
        Assert.AreEqual("Succeeded", root.GetProperty("outcome").GetString());
        Assert.AreEqual(artifactPath, root.GetProperty("artifactPath").GetString());
        Assert.AreEqual(184320, root.GetProperty("artifactBytes").GetInt64());
    }

    [TestMethod]
    public async Task Service_RunHistoryStoreTests_AppendBackupAsync_WhenTheArtifactObjectIsAbsentFallsBackToTheRecordsOwnPath()
    {
        var store = CreateStore();
        var recordedPath = ArtifactPathFor("mtm_mock", _windowUtc);

        await store.AppendBackupAsync(
            SucceededBackup(BackupStore.MtmMock, _windowUtc) with { ArtifactPath = recordedPath },
            artifact: null);

        var entry = (await store.ReadAsync()).Single();
        Assert.AreEqual(recordedPath, entry.ArtifactPath);
        Assert.IsNull(entry.ArtifactBytes, "no artifact means no size to claim");
    }

    [TestMethod]
    [DataRow(BackupRunOutcome.Succeeded, "Succeeded", null)]
    [DataRow(BackupRunOutcome.Never, "Pending", "neverRun")]
    [DataRow(BackupRunOutcome.ToolUnavailable, "Failed", "mysqldumpUnavailable")]
    [DataRow(BackupRunOutcome.Failed, "Failed", "mysqldumpFailed")]
    public async Task Service_RunHistoryStoreTests_AppendBackupAsync_MapsEachOutcomeOntoTheVocabularyTheCriteriaAreWrittenIn(
        BackupRunOutcome outcome,
        string expectedOutcome,
        string? expectedReason)
    {
        var store = CreateStore();

        await store.AppendBackupAsync(
            SucceededBackup(BackupStore.MtmReceivingApplication, _windowUtc) with { Outcome = outcome },
            artifact: null);

        var entry = (await store.ReadAsync()).Single();
        Assert.AreEqual(expectedOutcome, entry.Outcome);
        Assert.AreEqual(expectedReason, entry.Reason);
    }

    [TestMethod]
    public async Task Service_RunHistoryStoreTests_AppendBackupAsync_NamesEachStoreTheWayItsDatabaseIsNamed()
    {
        var store = CreateStore();

        foreach (var target in Enum.GetValues<BackupStore>())
        {
            await store.AppendBackupAsync(SucceededBackup(target, _windowUtc), artifact: null);
        }

        var names = (await store.ReadAsync()).Select(e => e.Store).ToList();
        CollectionAssert.AreEquivalent(
            new[] { "mtm_waitlist", "mtm_wip_application_winforms", "mtm_receiving_application", "mtm_mock" },
            names);
    }

    [TestMethod]
    public async Task Service_RunHistoryStoreTests_ReadAsync_ReturnsEveryEntryInTheOrderItWasWritten()
    {
        var store = CreateStore();

        await store.AppendRefreshAsync(SucceededRefresh("work_order_lookup", _cycleUtc, rows: 1));
        await store.AppendBackupAsync(SucceededBackup(BackupStore.MtmMock, _windowUtc), artifact: null);
        await store.AppendRefreshAsync(SucceededRefresh("operation_sequences", _cycleUtc, rows: 2));

        var entries = await store.ReadAsync();
        Assert.AreEqual(3, entries.Count);
        Assert.AreEqual(RunHistoryStore.RefreshRecordKind, entries[0].Record);
        Assert.AreEqual(RunHistoryStore.BackupRecordKind, entries[1].Record);
        Assert.AreEqual(RunHistoryStore.RefreshRecordKind, entries[2].Record);
    }

    [TestMethod]
    public async Task Service_RunHistoryStoreTests_ReadAsync_WhenTheFileDoesNotExistReturnsNothing()
    {
        Assert.AreEqual(0, (await CreateStore().ReadAsync()).Count);
    }

    [TestMethod]
    public async Task Service_RunHistoryStoreTests_ReadAsync_WhenOneLineIsCorruptKeepsTheRest()
    {
        var store = CreateStore();
        await store.AppendRefreshAsync(SucceededRefresh("work_order_lookup", _cycleUtc));
        await store.AppendRefreshAsync(SucceededRefresh("operation_sequences", _cycleUtc));

        var lines = (await ReadLinesAsync(store)).ToList();
        lines.Insert(1, "{ this is not json");
        await File.WriteAllLinesAsync(store.FilePath, lines);

        var entries = await store.ReadAsync();
        Assert.AreEqual(2, entries.Count, "one unreadable line is not a reason to report no history at all");
    }

    [TestMethod]
    public async Task Service_RunHistoryStoreTests_AppendAsync_DropsEntriesOlderThanTheRetentionWindow()
    {
        var store = CreateStore();

        await store.AppendRefreshAsync(
            SucceededRefresh("work_order_lookup", _nowUtc.AddDays(-(RunHistoryStore.RetentionDays + 1))));
        await store.AppendRefreshAsync(SucceededRefresh("operation_sequences", _nowUtc.AddDays(-1)));

        var entries = await store.ReadAsync();
        Assert.AreEqual(1, entries.Count, "the expired record is gone, the fresh one is not");
        Assert.AreEqual("operation_sequences", entries[0].Shape);
    }

    [TestMethod]
    public async Task Service_RunHistoryStoreTests_AppendAsync_WhenAnEntryIsExactlyAtTheCutoffKeepsIt()
    {
        var store = CreateStore();

        await store.AppendRefreshAsync(
            SucceededRefresh("work_order_lookup", _nowUtc.AddDays(-RunHistoryStore.RetentionDays)));

        Assert.AreEqual(
            1,
            (await store.ReadAsync()).Count,
            "retention removes what is older than the window, not what is on its edge");
    }

    [TestMethod]
    public async Task Service_RunHistoryStoreTests_AppendAsync_WhenALineCannotBeDatedKeepsIt()
    {
        var store = CreateStore();
        Directory.CreateDirectory(_root);
        await File.WriteAllLinesAsync(
            store.FilePath,
            new[] { "{\"record\":\"refresh\",\"shape\":\"work_order_lookup\",\"outcome\":\"Succeeded\"}" });

        await store.AppendRefreshAsync(SucceededRefresh("operation_sequences", _nowUtc.AddDays(-1)));

        Assert.AreEqual(
            2,
            (await ReadLinesAsync(store)).Count,
            "deleting evidence to save bytes is the wrong trade");
    }

    [TestMethod]
    public void Service_RunHistoryStoreTests_Retention_OutlivesTheWindowTheCriteriaAreMeasuredOver()
    {
        Assert.IsTrue(
            RunHistoryStore.RetentionDays > ReliabilityCriteria.MeasuredWindowDays,
            "retention must be longer than the window, or the first day of the window is pruned as it is read");
        Assert.AreEqual(ReliabilityCriteria.HistoryRetentionDays, RunHistoryStore.RetentionDays);
    }

    [TestMethod]
    public void Service_RunHistoryStoreTests_BackupRetention_OutlivesTheWindowTheCriteriaAreMeasuredOver()
    {
        Assert.IsTrue(
            ReliabilityCriteria.BackupRetentionCount >= ReliabilityCriteria.MeasuredWindowDays,
            "a store keeping fewer than 30 daily artifacts has pruned the artifact SC-008 asks about");
        Assert.AreEqual(
            ReliabilityCriteria.BackupRetentionCount,
            new BackupPolicy { Store = BackupStore.MtmMock, DestinationDirectory = @"C:\Backups" }.RetentionCount);
    }

    [TestMethod]
    public void Service_RunHistoryStoreTests_DiagnosticLogRetention_OutlivesTheWindowTheCriteriaAreMeasuredOver()
    {
        Assert.IsTrue(
            ServiceLog.RetentionDays > ReliabilityCriteria.MeasuredWindowDays,
            "a log that expires before the measured period cannot explain a failure inside it");
    }

    /// <summary>
    /// The store under test, always on this test's own clock. Nothing here falls back to the system clock: a store
    /// reading the real clock while the test writes an instant of its own is exactly how a record gets pruned as it
    /// is written.
    /// </summary>
    private RunHistoryStore CreateStore() =>
        new(_root, NullLogger<RunHistoryStore>.Instance, new FixedTimeProvider(_nowUtc));

    private static async Task<IReadOnlyList<string>> ReadLinesAsync(RunHistoryStore store) =>
        await File.ReadAllLinesAsync(store.FilePath);

    /// <summary>The instant as the file spells it, so no assertion restates a date.</summary>
    private static string Written(DateTime utc) =>
        utc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    /// <summary>The artifact path a backup of that window produces, named the way the backup engine names it.</summary>
    private static string ArtifactPathFor(string database, DateTime windowUtc) =>
        $@"C:\Backups\{database}\{windowUtc.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}.sql";

    private static DateTime TruncateToSecond(DateTime value) =>
        new(value.Ticks - (value.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);

    private static RefreshRunRecord SucceededRefresh(string shapeKey, DateTime cycleUtc, int rows = 0) => new()
    {
        ShapeKey = shapeKey,
        CycleUtc = cycleUtc,
        StartedUtc = cycleUtc,
        FinishedUtc = cycleUtc,
        Outcome = RefreshRunOutcome.Succeeded,
        RowCount = rows
    };

    private static BackupRunRecord SucceededBackup(BackupStore store, DateTime windowUtc) => new()
    {
        Store = store,
        StartedUtc = windowUtc,
        FinishedUtc = windowUtc,
        Outcome = BackupRunOutcome.Succeeded
    };

    /// <summary>Time source pinned to one instant, so retention is deterministic.</summary>
    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTime utcNow)
        {
            _utcNow = new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
