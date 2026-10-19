using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Module_Core.Services;

/// <summary>
/// The arrangement's two halves: the service's mapping of the arrangement read, and the one rule that turns that
/// arrangement into the <c>Lugger Location</c> cell's value (FR-009, FR-022, FR-024, FR-025, FR-026).
/// </summary>
/// <remarks>
/// The fixtures are the shipped defaults, so a case that fails here fails for the arrangement the application
/// actually starts with rather than for a hand-built one.
/// </remarks>
[TestClass]
public sealed class LuggerArrangementServiceTests
{
    private const string Door6 = "Door 6 Lugger";
    private const string Door7 = "Door 7 Lugger";
    private const string Door8 = "Door 8 Lugger";
    private const string Door9 = "Door 9 Lugger";
    private const string Extra = "Extra Lugger (Outside)";
    private const string Skeleton = "Skeleton Lugger";
    private const string Trailer = "Scrap Trailer";

    [TestMethod]
    public void Service_LuggerArrangementServiceTests_Snapshot_ResolvesTheSevenSeededAssignmentsToTheirLugger()
    {
        var snapshot = SeededSnapshot();

        Assert.AreEqual(Door6, snapshot.Resolve("3003 Aluminum").LuggerLocation);
        Assert.AreEqual(Door7, snapshot.Resolve("Galvanized Steel").LuggerLocation);
        Assert.AreEqual(Door8, snapshot.Resolve("5052 Aluminum").LuggerLocation);
        Assert.AreEqual(Door9, snapshot.Resolve("Steel").LuggerLocation);
        Assert.AreEqual(Skeleton, snapshot.Resolve("Skeleton").LuggerLocation);
        Assert.AreEqual(Trailer, snapshot.Resolve("Gaylord").LuggerLocation);
    }

    [TestMethod]
    public void Service_LuggerArrangementServiceTests_Snapshot_MatchesTheScrapTypeIgnoringCaseAndSurroundingSpaces()
    {
        var snapshot = SeededSnapshot();

        // The setup workflow spells this type `5052 aluminum` while the assignment is pinned as `5052 Aluminum`,
        // and a job value may arrive with either case or with spaces around it (FR-009, research D5).
        Assert.AreEqual(Door8, snapshot.Resolve("5052 aluminum").LuggerLocation);
        Assert.AreEqual(Door8, snapshot.Resolve("  5052 ALUMINUM  ").LuggerLocation);
        Assert.AreEqual(Door8, snapshot.Resolve("5052 Aluminum").LuggerLocation);
    }

    [TestMethod]
    public void Service_LuggerArrangementServiceTests_Snapshot_AnUnassignedScrapTypeResolvesNothingAndReportsTheGap()
    {
        var snapshot = SeededSnapshot();

        var answer = snapshot.Resolve("Brass");

        Assert.IsNull(answer.LuggerLocation);
        Assert.AreEqual(LuggerLocationOutcome.NotConfigured, answer.Outcome);
        Assert.IsTrue(answer.IsConfigurationGap, "A real scrap type with no lugger is the configuration gap FR-026 records.");
    }

    [TestMethod]
    public void Service_LuggerArrangementServiceTests_Snapshot_AJobWithNoScrapDecisionResolvesNothingAndIsNotAGap()
    {
        var snapshot = SeededSnapshot();

        foreach (var value in new string?[] { null, "", "   ", "Scrap Type Required", "No Scrap" })
        {
            var answer = snapshot.Resolve(value);

            Assert.IsNull(answer.LuggerLocation, $"'{value}' is not a scrap decision, so no lugger is named.");
            Assert.AreEqual(LuggerLocationOutcome.NoScrapDecision, answer.Outcome);
            Assert.IsFalse(answer.IsConfigurationGap, $"'{value}' is an ordinary absence, not a configuration gap.");
        }
    }

    [TestMethod]
    public void Service_LuggerArrangementServiceTests_Snapshot_ATypeWhoseLuggersAreAllBlockedNamesTheFirstWithItsStatus()
    {
        var rows = new[]
        {
            Row(Door9, "Full", "steel", "Steel", dumpPoint: false, sortPosition: 1),
            Row(Door7, "Do Not Use", "steel", "Steel", dumpPoint: false, sortPosition: 2),
        };

        var answer = LuggerArrangementSnapshot.Create(rows).Resolve("steel");

        Assert.AreEqual("Door 9 Lugger (Full)", answer.LuggerLocation);
        Assert.AreEqual(LuggerLocationOutcome.Resolved, answer.Outcome);
    }

    [TestMethod]
    public void Service_LuggerArrangementServiceTests_GetSnapshotAsync_MapsTheArrangementReadInOneCall()
    {
        var stub = new StubMySqlHelperServer(SeededRows().Append(Row(Trailer, "Available", null, null, null, null)).ToArray());
        var service = new LuggerArrangementService(
            stub,
            Options.Create(new WaitlistDatabaseOptions { ConnectionString = "Server=unused;" }));

        var snapshot = service.GetSnapshotAsync().GetAwaiter().GetResult();

        Assert.AreEqual(1, stub.QueryCallCount, "The list load reads the arrangement once, not once per card.");
        Assert.AreEqual("sp_waitlist_lugger_arrangement_list", stub.LastProcedureName);
        Assert.AreEqual(8, snapshot.Rows.Count, "Seven pairings plus the lugger that takes no scrap type.");
        Assert.AreEqual(6, snapshot.ScrapTypes.Count, "Six distinct scrap types are assigned in the defaults.");

        var unassigned = snapshot.Rows.Single(row => row.LuggerName == Trailer && row.ScrapTypeKey is null);
        Assert.IsNull(unassigned.ScrapType);
        Assert.IsNull(unassigned.IsDumpPoint);
        Assert.IsNull(unassigned.SortPosition);
    }

    private static LuggerArrangementSnapshot SeededSnapshot() => LuggerArrangementSnapshot.Create(SeededRows());

    /// <summary>The seven pairings the default seed writes, in the arrangement read's column shape.</summary>
    private static IReadOnlyList<ScrapAssignment> SeededRows() =>
    [
        Row(Door6, "Available", "3003 aluminum", "3003 Aluminum", dumpPoint: true, sortPosition: 1, position: 1),
        Row(Extra, "Backup", "3003 aluminum", "3003 Aluminum", dumpPoint: false, sortPosition: 2, position: 5),
        Row(Door7, "Available", "galvanized steel", "Galvanized Steel", dumpPoint: true, sortPosition: 1, position: 2),
        Row(Door8, "Available", "5052 aluminum", "5052 Aluminum", dumpPoint: true, sortPosition: 1, position: 3),
        Row(Door9, "Available", "steel", "Steel", dumpPoint: true, sortPosition: 1, position: 4),
        Row(Skeleton, "Available", "skeleton", "Skeleton", dumpPoint: true, sortPosition: 1, position: 6),
        Row(Trailer, "Available", "gaylord", "Gaylord", dumpPoint: true, sortPosition: 1, position: 7),
    ];

    private static ScrapAssignment Row(
        string luggerName,
        string status,
        string? scrapTypeKey,
        string? scrapType,
        bool? dumpPoint,
        int? sortPosition,
        int position = 1,
        string? reason = null) => new()
        {
            AssignmentPublicId = scrapTypeKey is null ? null : $"assignment-{luggerName}-{scrapTypeKey}",
            LuggerPublicId = $"lugger-{luggerName}",
            LuggerName = luggerName,
            LuggerStatus = status,
            LuggerStatusReason = reason,
            LuggerDisplayPosition = position,
            ScrapType = scrapType,
            ScrapTypeKey = scrapTypeKey,
            IsDumpPoint = dumpPoint,
            SortPosition = sortPosition,
        };

    /// <summary>
    /// The stubbed seam: answers one arrangement read and records what was asked, so the "one call per load"
    /// claim is checked rather than assumed.
    /// </summary>
    private sealed class StubMySqlHelperServer : IMySqlHelperServer
    {
        private readonly IReadOnlyList<ScrapAssignment> _assignments;

        public StubMySqlHelperServer(IReadOnlyList<ScrapAssignment> assignments) => _assignments = assignments;

        public int QueryCallCount { get; private set; }

        public string? LastProcedureName { get; private set; }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            QueryCallCount++;
            LastProcedureName = storedProcedureName;

            return Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(
                _assignments.Select(ToRow).ToArray());
        }

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The arrangement service never sends statement text.");

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The arrangement service never sends statement text.");

        private static Dictionary<string, object?> ToRow(ScrapAssignment assignment) => new()
        {
            ["assignment_public_id"] = assignment.AssignmentPublicId,
            ["lugger_public_id"] = assignment.LuggerPublicId,
            ["lugger_name"] = assignment.LuggerName,
            ["status"] = assignment.LuggerStatus,
            ["status_reason"] = assignment.LuggerStatusReason,
            ["lugger_display_position"] = assignment.LuggerDisplayPosition,
            ["scrap_type"] = assignment.ScrapType,
            ["scrap_type_key"] = assignment.ScrapTypeKey,
            ["is_dump_point"] = assignment.IsDumpPoint is null ? null : assignment.IsDumpPoint.Value ? 1 : 0,
            ["assignment_sort_position"] = assignment.SortPosition,
        };
    }
}
