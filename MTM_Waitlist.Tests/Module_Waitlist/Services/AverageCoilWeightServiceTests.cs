using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

/// <summary>
/// The average-coil-weight read goes to the receiving store on every call: the demo short-circuit and its
/// sample catalog were retired, so there is no cached or sample path left to exercise (FR-001, FR-014, SC-013).
/// </summary>
[TestClass]
public sealed class AverageCoilWeightServiceTests
{
    [TestMethod]
    public async Task Resolve_RunsQuery_AndFormatsAverage()
    {
        var helper = new StubMySqlHelperServer(rows: new[] { AverageRow(5200m) });
        var service = new AverageCoilWeightService(helper);

        var result = await service.ResolveAverageCoilWeightTextAsync("MMC0001000");

        Assert.AreEqual("5,200 lb", result);
        Assert.AreEqual(1, helper.QueryCallCount);
        Assert.AreEqual(MySqlDatabaseTarget.MtmReceivingApplication, helper.LastDatabaseTarget);
    }

    [TestMethod]
    public async Task Resolve_UsesTheReceivingStoredProcedure_AndNoInlineStatement()
    {
        var helper = new StubMySqlHelperServer(rows: new[] { AverageRow(5200m) });
        var service = new AverageCoilWeightService(helper);

        await service.ResolveAverageCoilWeightTextAsync("MMC0001000");

        // FR-015 / constitution III: the read goes through the receiving store's procedure, and no
        // statement text is executed from the application. This is the regression guard for T088 —
        // reintroducing the inline SELECT would fail both assertions.
        Assert.AreEqual(
            "sp_receiving_history_average_coil_weight",
            helper.LastStoredProcedureName,
            "The average coil weight must be read through the receiving store's procedure.");
        Assert.AreEqual(0, helper.SqlCallCount, "No inline statement text may be executed.");
        Assert.AreEqual("MMC0001000", helper.LastParameters?["@p_part_id"], "The procedure takes the part id.");
    }

    [TestMethod]
    public async Task Resolve_RoundsToWholePounds()
    {
        var helper = new StubMySqlHelperServer(rows: new[] { AverageRow(5199.6m) });
        var service = new AverageCoilWeightService(helper);

        var result = await service.ResolveAverageCoilWeightTextAsync("MMC0001000");

        Assert.AreEqual("5,200 lb", result);
    }

    [TestMethod]
    public async Task Resolve_NoRows_ReturnsEmpty()
    {
        var helper = new StubMySqlHelperServer(rows: Array.Empty<Dictionary<string, object?>>());
        var service = new AverageCoilWeightService(helper);

        var result = await service.ResolveAverageCoilWeightTextAsync("MMC0001000");

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public async Task Resolve_EmptyPart_ReturnsEmptyWithoutQuery()
    {
        var helper = new StubMySqlHelperServer(rows: Array.Empty<Dictionary<string, object?>>());
        var service = new AverageCoilWeightService(helper);

        var result = await service.ResolveAverageCoilWeightTextAsync("   ");

        Assert.AreEqual(string.Empty, result);
        Assert.AreEqual(0, helper.QueryCallCount);
    }

    private static Dictionary<string, object?> AverageRow(decimal average) => new()
    {
        ["AverageWeight"] = average,
    };

    private sealed class StubMySqlHelperServer : IMySqlHelperServer
    {
        private readonly IReadOnlyList<Dictionary<string, object?>> _rows;

        public StubMySqlHelperServer(IReadOnlyList<Dictionary<string, object?>> rows) => _rows = rows;

        /// <summary>Every read, whether it went through a procedure or through inline statement text.</summary>
        public int QueryCallCount { get; private set; }

        /// <summary>Reads issued as raw statement text — must stay 0 (FR-015).</summary>
        public int SqlCallCount { get; private set; }

        public MySqlDatabaseTarget? LastDatabaseTarget { get; private set; }

        public string? LastStoredProcedureName { get; private set; }

        public IReadOnlyDictionary<string, object?>? LastParameters { get; private set; }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            QueryCallCount++;
            LastStoredProcedureName = storedProcedureName;
            LastParameters = parameters;
            LastDatabaseTarget = databaseTarget;
            return Task.FromResult(_rows);
        }

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            QueryCallCount++;
            SqlCallCount++;
            LastDatabaseTarget = databaseTarget;
            return Task.FromResult(_rows);
        }

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(0);
    }
}
