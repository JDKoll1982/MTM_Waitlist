using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

[TestClass]
public sealed class AverageCoilWeightServiceTests
{
    [TestMethod]
    public async Task Resolve_MockOn_ForSeededCoilPart_ReturnsSampleWeight()
    {
        var settings = new InMemoryLocalSettingsService(recvMockData: true);
        var helper = new StubMySqlHelperServer(rows: new[] { AverageRow(5200m) });
        var service = new AverageCoilWeightService(settings, helper);

        var result = await service.ResolveAverageCoilWeightTextAsync("MMC0001000");

        Assert.AreEqual("5,000 lb", result);
        Assert.AreEqual(0, helper.QueryCallCount, "Mock ON must not touch the database.");
    }

    [TestMethod]
    public async Task Resolve_MockOn_UnknownPart_ReturnsEmpty()
    {
        var settings = new InMemoryLocalSettingsService(recvMockData: true);
        var helper = new StubMySqlHelperServer(rows: new[] { AverageRow(5200m) });
        var service = new AverageCoilWeightService(settings, helper);

        var result = await service.ResolveAverageCoilWeightTextAsync("SOME-OTHER");

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public async Task Resolve_MockOff_RunsQuery_AndFormatsAverage()
    {
        var settings = new InMemoryLocalSettingsService(recvMockData: false);
        var helper = new StubMySqlHelperServer(rows: new[] { AverageRow(5200m) });
        var service = new AverageCoilWeightService(settings, helper);

        var result = await service.ResolveAverageCoilWeightTextAsync("MMC0001000");

        Assert.AreEqual("5,200 lb", result);
        Assert.AreEqual(1, helper.QueryCallCount);
        Assert.AreEqual(MySqlDatabaseTarget.MtmReceivingApplication, helper.LastDatabaseTarget);
    }

    [TestMethod]
    public async Task Resolve_MockOff_NoRows_ReturnsEmpty()
    {
        var settings = new InMemoryLocalSettingsService(recvMockData: false);
        var helper = new StubMySqlHelperServer(rows: Array.Empty<Dictionary<string, object?>>());
        var service = new AverageCoilWeightService(settings, helper);

        var result = await service.ResolveAverageCoilWeightTextAsync("MMC0001000");

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public async Task Resolve_EmptyPart_ReturnsEmptyWithoutQuery()
    {
        var settings = new InMemoryLocalSettingsService(recvMockData: false);
        var helper = new StubMySqlHelperServer(rows: Array.Empty<Dictionary<string, object?>>());
        var service = new AverageCoilWeightService(settings, helper);

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

    private sealed class InMemoryLocalSettingsService : ILocalSettingsService
    {
        private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);

        public InMemoryLocalSettingsService(bool recvMockData)
        {
            _values["Feature.RecvMockData"] = recvMockData;
        }

        public Task<T?> ReadSettingAsync<T>(string key)
        {
            if (_values.TryGetValue(key, out var value) && value is T typed)
            {
                return Task.FromResult<T?>(typed);
            }

            return Task.FromResult<T?>(default);
        }

        public Task SaveSettingAsync<T>(string key, T value)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default)
        {
            _values.Remove(key);
            return Task.CompletedTask;
        }

        public Task ResetAsync()
        {
            _values.Clear();
            return Task.CompletedTask;
        }

        public Task CorruptForTestAsync() => Task.CompletedTask;
    }
}
