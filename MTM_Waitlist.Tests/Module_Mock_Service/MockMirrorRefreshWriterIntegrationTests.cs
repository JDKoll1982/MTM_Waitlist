using System.Text.Json;

using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MySqlConnector;

using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Service.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// Live-database proof that a payload written through <see cref="MockMirrorRefreshWriter"/> reaches the real
/// <c>sp_visual_&lt;shape&gt;_refresh</c>, that the swap is atomic, and that the reported row count matches the
/// loaded snapshot (FR-006/SC-005/FR-013, tasks T118 and T064's database-level half).
/// </summary>
/// <remarks>
/// <para>
/// <b>Requires</b> <c>MTM_WAITLIST_TEST_DB_CONNECTION_STRING</c> to point at a MySQL instance where
/// <c>mtm_mock</c> is deployed (the connection's database is rewritten to <c>mtm_mock</c>; the procedures
/// resolve their own schema through <c>DATABASE()</c>). <c>MTM_MOCK_DB_CONNECTION_STRING</c> overrides that
/// when the cache lives on a different instance. Without the test variable the test reports inconclusive so
/// the suite stays green offline, which is the same convention the other live-database suites use.
/// </para>
/// <para>
/// <b>Atomicity is proven by the result of the swap, not by racing a reader.</b> The three-name
/// <c>RENAME TABLE</c> leaves the outgoing snapshot in the stage twin, so after a refresh the stage twin must
/// hold <i>exactly</i> the rows the live table held before it. A truncate-and-reload, or a swap that moved a
/// partial set, could not produce that — and unlike a concurrent read, it is deterministic rather than a race
/// that passes by timing.
/// </para>
/// <para>
/// <b>This test mutates the cache, and puts it back.</b> It replaces one shape's snapshot in <c>mtm_mock</c>.
/// "The on-host service will re-warm it" is true on the host but <b>false on a workstation</b>, where no
/// service runs: an unrestored run leaves the live mirror empty and silently breaks the app's fallback for
/// that shape (observed 2026-09-11). The snapshot is therefore captured in <c>TestInitialize</c> and written
/// back through the same procedure in <c>TestCleanup</c>, so the shared cache is handed back as found. It
/// touches no internal store and never touches <c>mtm_waitlist</c>, the WIP store, or the receiving store
/// (FR-027).
/// </para>
/// <para>
/// <b>Restoration is data-complete but not byte-identical.</b> <c>sp_visual_&lt;shape&gt;_refresh</c> hardcodes
/// <c>is_seed_content = 0</c> and stamps a fresh <c>refreshed_utc</c>, so a restored snapshot cannot
/// reproduce those two columns. Which leads to the next rule.
/// </para>
/// <para>
/// <b>A seed baseline is never mutated.</b> The unrefreshed baseline a fresh install depends on (FR-017) is
/// exactly the case the procedure cannot put back, and destroying it is precisely the harm to avoid. When
/// the captured snapshot contains seed rows the test reports inconclusive <b>without mutating anything</b>
/// rather than run and leave the baseline damaged.
/// </para>
/// </remarks>
[TestClass]
public sealed class MockMirrorRefreshWriterIntegrationTests
{
    private const string ConnectionStringVariable = "MTM_WAITLIST_TEST_DB_CONNECTION_STRING";

    /// <summary>The dedicated cache variable <c>MySqlHelperServer</c> prefers for the <c>mtm_mock</c> target.</summary>
    private const string MockConnectionStringVariable = "MTM_MOCK_DB_CONNECTION_STRING";

    private const string MirrorTable = "visual_work_order_lookup_result";
    private const string StageTable = "visual_work_order_lookup_result_stage";
    private const string PreviousTable = "visual_work_order_lookup_result_prev";

    private MockMirrorRefreshWriter _writer = null!;
    private MySqlHelperServer _reader = null!;
    private VisualReadShape _shape = null!;
    private MirrorSnapshot? _preTestSnapshot;

    [TestInitialize]
    public async Task TestInitializeAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive($"{ConnectionStringVariable} is not set; skipping the live mtm_mock integration test.");
        }

        var options = Options.Create(new StartupDatabaseOptions { ConnectionString = connectionString });

        // The writer must address mtm_mock, but the waitlist connection string names mtm_waitlist. Override
        // the database exactly as MySqlHelperServer does for MySqlDatabaseTarget.MtmMock; without it the
        // procedure call resolves against the wrong schema and fails with
        // "PROCEDURE mtm_waitlist.sp_visual_<shape>_refresh does not exist".
        _writer = new MockMirrorRefreshWriter(ResolveMockConnectionString(connectionString!));
        _reader = new MySqlHelperServer(options);

        _shape = MTM_Waitlist.Mock.Services.VisualReadShapeCatalog.Create()
            .Single(shape => shape.Key == "work_order_lookup");

        // Capture BEFORE anything is mutated, so TestCleanup can hand the shared cache back as found.
        _preTestSnapshot = await CaptureSnapshotAsync(MirrorTable).ConfigureAwait(false);

        if (_preTestSnapshot.SeedRowCount > 0)
        {
            // See the class remarks: a seed baseline cannot be restored through the refresh procedure
            // (is_seed_content is hardcoded to 0), so replacing it would destroy a fresh install's fallback
            // data with no way back. Mutate nothing instead.
            Assert.Inconclusive(
                $"{MirrorTable} holds {_preTestSnapshot.SeedRowCount} seed row(s) — an unrefreshed baseline. " +
                "This test cannot restore is_seed_content through the refresh procedure, so it leaves the " +
                "cache untouched instead of replacing a baseline it could not put back.");
        }
    }

    [TestCleanup]
    public async Task TestCleanupAsync()
    {
        var snapshot = _preTestSnapshot;
        if (snapshot is null)
        {
            // TestInitialize reported inconclusive (no connection string, or a seed baseline) before
            // capturing anything; no mutation happened, so there is nothing to put back.
            return;
        }

        // Hand the shared cache back as found. On a workstation no on-host service will re-warm it, which is
        // exactly how the live mirror was left empty on 2026-09-11.
        await _writer.RefreshAsync(_shape, snapshot.Json).ConfigureAwait(false);

        var restored = await ReadSnapshotAsync(MirrorTable).ConfigureAwait(false);

        CollectionAssert.AreEqual(
            snapshot.Projection,
            restored,
            "After the test the shared mtm_mock snapshot must be exactly what it was before.");
    }

    /// <summary>
    /// Resolves the <c>mtm_mock</c> connection string: the dedicated mock variable when it is set (the same
    /// precedence <c>MySqlHelperServer</c> applies), otherwise the test connection string with its database
    /// overridden to <c>mtm_mock</c>.
    /// </summary>
    private static string ResolveMockConnectionString(string connectionString)
    {
        var dedicated = Environment.GetEnvironmentVariable(MockConnectionStringVariable)?.Trim();

        return new MySqlConnectionStringBuilder(
            string.IsNullOrWhiteSpace(dedicated) ? connectionString : dedicated)
        {
            Database = "mtm_mock",
        }.ConnectionString;
    }

    [TestMethod]
    public async Task RefreshAsync_ReachesTheRealProcedure_ReportsTheLoadedRowCount_AndSwapsAtomically()
    {
        var runId = Guid.NewGuid().ToString("N")[..8];
        var previousSnapshot = await ReadSnapshotAsync(MirrorTable).ConfigureAwait(false);

        var payload = JsonSerializer.Serialize(new[]
        {
            new
            {
                NormalizedWorkOrder = $"ITEST-A-{runId}",
                PartNumber = $"P-A-{runId}",
                Description = "integration row A",
                WorkCenter = "WC-ITEST",
            },
            new
            {
                NormalizedWorkOrder = $"ITEST-B-{runId}",
                PartNumber = $"P-B-{runId}",
                Description = "integration row B",
                WorkCenter = "WC-ITEST",
            },
        });

        var reportedRowCount = await _writer.RefreshAsync(_shape, payload).ConfigureAwait(false);

        Assert.AreEqual(2, reportedRowCount, "The writer must report the payload's row count (FR-013).");

        // The live table now holds exactly the payload — the procedure accepted it and validated the load.
        var live = await ReadSnapshotAsync(MirrorTable).ConfigureAwait(false);
        CollectionAssert.AreEqual(
            new[] { $"ITEST-A-{runId}|P-A-{runId}", $"ITEST-B-{runId}|P-B-{runId}" },
            live,
            "The live mirror must hold exactly the snapshot that was handed to the procedure.");

        // Atomic swap: the outgoing snapshot is in the stage twin, complete — never a partial set, never empty.
        var stage = await ReadSnapshotAsync(StageTable).ConfigureAwait(false);
        CollectionAssert.AreEqual(
            previousSnapshot,
            stage,
            "After the three-name RENAME the stage twin must hold the complete outgoing snapshot.");

        // The three-name idiom's transient `_prev` name must not survive the swap.
        var previousTableCount = await CountTableAsync(PreviousTable).ConfigureAwait(false);
        Assert.AreEqual(0L, previousTableCount, "The transient '_prev' table must not persist between cycles.");

        // No seed content may survive a real refresh (FR-017): the snapshot is live data now.
        var seedRows = await _reader.ExecuteSqlQueryAsync(
            $"SELECT COUNT(*) AS seed_rows FROM {MirrorTable} WHERE is_seed_content = 1",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmMock).ConfigureAwait(false);

        Assert.AreEqual(
            0L,
            Convert.ToInt64(seedRows[0]["seed_rows"]),
            "A refreshed mirror must hold no seed rows.");
    }

    [TestMethod]
    public async Task RefreshAsync_WithAnEmptyPayload_IsALegitimateEmptySnapshot()
    {
        // An empty result set is a real answer from Infor Visual (FR-024) and must be storable, not rejected.
        var reportedRowCount = await _writer.RefreshAsync(_shape, "[]").ConfigureAwait(false);

        Assert.AreEqual(0, reportedRowCount);

        var live = await ReadSnapshotAsync(MirrorTable).ConfigureAwait(false);
        Assert.AreEqual(0, live.Length, "An empty payload produces an empty — and complete — snapshot.");
    }

    /// <summary>Reads the mirror's projected columns in a stable order, so two reads are comparable.</summary>
    private async Task<string[]> ReadSnapshotAsync(string table)
    {
        var rows = await _reader.ExecuteSqlQueryAsync(
            $"SELECT normalized_work_order, part_number FROM {table} ORDER BY normalized_work_order",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmMock).ConfigureAwait(false);

        return rows
            .Select(row => $"{row["normalized_work_order"]}|{row["part_number"]}")
            .ToArray();
    }

    /// <summary>
    /// Captures everything needed to put the shared mirror back: the rows in the key names the refresh
    /// procedure expects, the projected order to compare against afterwards, and how many of those rows are
    /// seed content (which decides whether the test may run at all).
    /// </summary>
    private async Task<MirrorSnapshot> CaptureSnapshotAsync(string table)
    {
        var rows = await _reader.ExecuteSqlQueryAsync(
            $"SELECT normalized_work_order, part_number, description, work_center, is_seed_content FROM {table} " +
            "ORDER BY normalized_work_order, part_number",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmMock).ConfigureAwait(false);

        var payload = rows.Select(row => new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["NormalizedWorkOrder"] = row["normalized_work_order"],
            ["PartNumber"] = row["part_number"],
            ["Description"] = ReadNullableColumn(row, "description"),
            ["WorkCenter"] = ReadNullableColumn(row, "work_center"),
        });

        return new MirrorSnapshot(
            JsonSerializer.Serialize(payload),
            rows.Select(row => $"{row["normalized_work_order"]}|{row["part_number"]}").ToArray(),
            rows.Count(row => Convert.ToInt64(row["is_seed_content"]) == 1L));
    }

    /// <summary>
    /// Reads a nullable column as <see langword="null" /> rather than <see cref="DBNull" />; a JSON null is
    /// what the procedure turns back into SQL NULL on the way in.
    /// </summary>
    private static object? ReadNullableColumn(Dictionary<string, object?> row, string column) =>
        row.TryGetValue(column, out var value) && value is not DBNull ? value : null;

    /// <summary>What the shared mirror held before a test replaced it.</summary>
    private sealed record MirrorSnapshot(string Json, string[] Projection, int SeedRowCount);

    private async Task<long> CountTableAsync(string tableName)
    {
        var rows = await _reader.ExecuteSqlQueryAsync(
            "SELECT COUNT(*) AS table_count FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = @p_table",
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["@p_table"] = tableName },
            MySqlDatabaseTarget.MtmMock).ConfigureAwait(false);

        return rows.Count == 0 ? 0L : Convert.ToInt64(rows[0]["table_count"]);
    }
}
