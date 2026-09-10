using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

/// <summary>
/// The average-coil-weight read goes to the receiving store on every call: the <c>Feature.RecvMockData</c>
/// short-circuit and its sample catalog were retired (FR-001, FR-014, SC-013).
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

        public int QueryCallCount { get; private set; }

        public MySqlDatabaseTarget? LastDatabaseTarget { get; private set; }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_rows);

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
