using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class MockConfigurationServiceTests
{
    [TestMethod]
    public async Task GetMockSettingAsync_NoRow_ReturnsNotPresent_False()
    {
        var stub = new StubMySqlHelperServer(Array.Empty<Dictionary<string, object?>>());
        var service = new MockConfigurationService(stub);

        var state = await service.GetMockSettingAsync(ConnectionSource.InforVisual);

        Assert.IsFalse(state.IsPresent);
        Assert.IsFalse(state.IsMockEnabled);
        Assert.AreEqual("Feature.InforVisualMockData", state.SettingKey);
    }

    [TestMethod]
    public async Task GetMockSettingAsync_BoolRow_ReturnsEnabled()
    {
        var row = new Dictionary<string, object?> { ["setting_value_bool"] = (byte)1 };
        var service = new MockConfigurationService(new StubMySqlHelperServer(new[] { row }));

        var state = await service.GetMockSettingAsync(ConnectionSource.Receiving);

        Assert.IsTrue(state.IsPresent);
        Assert.IsTrue(state.IsMockEnabled);
    }

    [TestMethod]
    public async Task SetMockSettingAsync_UpsertsCentralRow_ForSource()
    {
        var stub = new StubMySqlHelperServer(Array.Empty<Dictionary<string, object?>>());
        var service = new MockConfigurationService(stub);

        var ok = await service.SetMockSettingAsync(ConnectionSource.InforVisual, enabled: true, updatedByUserId: 42L);

        Assert.IsTrue(ok);
        Assert.AreEqual("sp_config_settings_upsert", stub.LastNonQueryProcedure);
        Assert.AreEqual("Feature.InforVisualMockData", stub.LastNonQueryParameters!["p_setting_key"]);
        Assert.AreEqual(true, stub.LastNonQueryParameters["p_setting_value_bool"]);
        Assert.AreEqual("all_users", stub.LastNonQueryParameters["p_scope_type"]);
        Assert.AreEqual(42L, stub.LastNonQueryParameters["p_updated_by_user_id"]);
    }

    private sealed class StubMySqlHelperServer : IMySqlHelperServer
    {
        private readonly IReadOnlyList<Dictionary<string, object?>> _rows;

        public StubMySqlHelperServer(IReadOnlyList<Dictionary<string, object?>> rows) => _rows = rows;

        public string? LastNonQueryProcedure { get; private set; }

        public IReadOnlyDictionary<string, object?>? LastNonQueryParameters { get; private set; }

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
        {
            LastNonQueryProcedure = storedProcedureName;
            LastNonQueryParameters = new Dictionary<string, object?>(parameters, StringComparer.OrdinalIgnoreCase);
            return Task.FromResult(1);
        }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_rows);

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(0);
    }
}
