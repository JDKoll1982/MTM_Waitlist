using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

/// <summary>
/// QA coverage for the central mock-config write path + client refresh apply (F13 Phase 2.2): central
/// read/write per source, role gating, a changed central value being applied on the next client refresh with a
/// single event, and last-writer-wins concurrent upserts.
/// </summary>
[TestClass]
public sealed class MockModeQaTests
{
    [TestMethod]
    public async Task CentralReadWrite_MapPerSourceKey_AndScopeAllUsers()
    {
        var stub = new RecordingStubHelper(Array.Empty<Dictionary<string, object?>>());
        var config = new MockConfigurationService(stub);

        // Read Receiving -> Feature.RecvMockData.
        _ = await config.GetMockSettingAsync(ConnectionSource.Receiving);
        Assert.AreEqual("sp_config_settings_get_effective", stub.LastQueryProcedure);
        Assert.AreEqual("Feature.RecvMockData", stub.LastQueryParameters!["p_setting_key"]);

        // Write InforVisual -> Feature.InforVisualMockData at all_users/bool.
        _ = await config.SetMockSettingAsync(ConnectionSource.InforVisual, enabled: true, updatedByUserId: 7L);
        Assert.AreEqual("sp_config_settings_upsert", stub.LastNonQueryProcedure);
        Assert.AreEqual("Feature.InforVisualMockData", stub.LastNonQueryParameters!["p_setting_key"]);
        Assert.AreEqual("all_users", stub.LastNonQueryParameters["p_scope_type"]);
        Assert.AreEqual("bool", stub.LastNonQueryParameters["p_value_type"]);
        Assert.AreEqual(true, stub.LastNonQueryParameters["p_setting_value_bool"]);
        Assert.AreEqual(7L, stub.LastNonQueryParameters["p_updated_by_user_id"]);
    }

    [TestMethod]
    public void RoleGating_DeniesOperatorRole_ForConfigAndCatalogEdits()
    {
        var guard = new DeveloperAccessGuard();

        Assert.IsTrue(guard.CanChangeCentralMockConfig("developer"));
        Assert.IsTrue(guard.CanEditRealCatalog("Admin"));
        Assert.IsFalse(guard.CanChangeCentralMockConfig("material handler"));
        Assert.IsFalse(guard.CanEditRealCatalog("supervisor"));
    }

    [TestMethod]
    public async Task ClientRefresh_AppliesChangedCentralValue_AndRaisesOneEvent()
    {
        var env = new RefreshHarness(threshold: 1, reachable: true);
        env.Config.Set(ConnectionSource.Receiving, false);
        var refresh = env.CreateRefreshService();
        var monitor = new MockRoutingMonitorService(refresh);
        var raised = new List<MockModeChange>();
        monitor.MockModeChanged += (_, change) => raised.Add(change);

        await monitor.RunOnceAsync(); // baseline: live, no events
        Assert.AreEqual(0, raised.Count);

        env.Config.Set(ConnectionSource.Receiving, true); // central config flips while reachable
        await monitor.RunOnceAsync();

        Assert.AreEqual(1, raised.Count);
        Assert.AreEqual(MockModeChangeKind.TurnedOn, raised[0].Kind);
        Assert.IsTrue(refresh.LastDecisions.ContainsKey(ConnectionSource.Receiving));
        Assert.IsTrue(refresh.LastDecisions[ConnectionSource.Receiving].UseMockData);
    }

    [TestMethod]
    public async Task ConcurrentUpserts_AreLastWriterWins_OnSameKey()
    {
        var stub = new RecordingStubHelper(Array.Empty<Dictionary<string, object?>>());
        var config = new MockConfigurationService(stub);

        // Two clients write the same source; each upsert is independent and the DB applies last-writer-wins.
        _ = await config.SetMockSettingAsync(ConnectionSource.Receiving, enabled: true, updatedByUserId: 1L);
        _ = await config.SetMockSettingAsync(ConnectionSource.Receiving, enabled: false, updatedByUserId: 2L);

        Assert.AreEqual(2, stub.NonQueryCalls.Count);
        // Last call carries the newest value -> wins at the DB.
        var last = stub.NonQueryCalls[^1].Parameters;
        Assert.AreEqual("Feature.RecvMockData", last["p_setting_key"]);
        Assert.AreEqual(false, last["p_setting_value_bool"]);
        Assert.AreEqual(2L, last["p_updated_by_user_id"]);
    }

    private sealed class RefreshHarness
    {
        private readonly MockFallbackDebouncer _debouncer;

        public RefreshHarness(int threshold, bool reachable)
        {
            _debouncer = new MockFallbackDebouncer(threshold);
            Health = new HarnessHealth(reachable);
            Config = new HarnessConfig();
            Local = new HarnessLocalSettings();
        }

        public HarnessHealth Health { get; }

        public HarnessConfig Config { get; }

        public HarnessLocalSettings Local { get; }

        public MockRoutingRefreshService CreateRefreshService()
            => new(Health, Config, new MockRoutingService(), Local, _debouncer);
    }

    private sealed class HarnessHealth : IConnectionHealthService
    {
        private readonly bool _reachable;

        public HarnessHealth(bool reachable) => _reachable = reachable;

        public Task<ConnectionHealthState> CheckAsync(ConnectionSource source, CancellationToken cancellationToken = default)
            => Task.FromResult(new ConnectionHealthState { Source = source, Status = _reachable ? ConnectionHealthStatus.Connected : ConnectionHealthStatus.Unreachable, CheckedUtc = DateTimeOffset.UtcNow });

        public Task<IReadOnlyDictionary<ConnectionSource, ConnectionHealthState>> CheckAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<ConnectionSource, ConnectionHealthState>>(new Dictionary<ConnectionSource, ConnectionHealthState>());

        public ConnectionHealthState GetLastKnown(ConnectionSource source)
            => new() { Source = source, Status = _reachable ? ConnectionHealthStatus.Connected : ConnectionHealthStatus.Unreachable };
    }

    private sealed class HarnessConfig : IMockConfigurationService
    {
        private readonly Dictionary<ConnectionSource, bool> _enabled = new() { [ConnectionSource.Receiving] = false, [ConnectionSource.InforVisual] = false };

        public void Set(ConnectionSource source, bool enabled) => _enabled[source] = enabled;

        public Task<MockSettingState> GetMockSettingAsync(ConnectionSource source, CancellationToken cancellationToken = default)
            => Task.FromResult(new MockSettingState { SettingKey = MockSettingKeys.For(source), IsPresent = true, IsMockEnabled = _enabled[source] });

        public Task<bool> SetMockSettingAsync(ConnectionSource source, bool enabled, long? updatedByUserId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class HarnessLocalSettings : ILocalSettingsService
    {
        public Task<T?> ReadSettingAsync<T>(string key) => Task.FromResult<T?>(default);

        public Task SaveSettingAsync<T>(string key, T value) => Task.CompletedTask;

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ResetAsync() => Task.CompletedTask;

        public Task CorruptForTestAsync() => Task.CompletedTask;
    }

    private sealed class RecordingStubHelper : IMySqlHelperServer
    {
        private readonly IReadOnlyList<Dictionary<string, object?>> _rows;

        public RecordingStubHelper(IReadOnlyList<Dictionary<string, object?>> rows) => _rows = rows;

        public List<(string Procedure, IReadOnlyDictionary<string, object?> Parameters)> NonQueryCalls { get; } = new();

        public string? LastQueryProcedure { get; private set; }

        public IReadOnlyDictionary<string, object?>? LastQueryParameters { get; private set; }

        public string? LastNonQueryProcedure { get; private set; }

        public IReadOnlyDictionary<string, object?>? LastNonQueryParameters { get; private set; }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(string storedProcedureName, IReadOnlyDictionary<string, object?> parameters, MySqlDatabaseTarget databaseTarget, CancellationToken cancellationToken = default)
        {
            LastQueryProcedure = storedProcedureName;
            LastQueryParameters = parameters;
            return Task.FromResult(_rows);
        }

        public Task<int> ExecuteStoredProcedureNonQueryAsync(string storedProcedureName, IReadOnlyDictionary<string, object?> parameters, MySqlDatabaseTarget databaseTarget, CancellationToken cancellationToken = default)
        {
            LastNonQueryProcedure = storedProcedureName;
            LastNonQueryParameters = new Dictionary<string, object?>(parameters, StringComparer.OrdinalIgnoreCase);
            NonQueryCalls.Add((storedProcedureName, LastNonQueryParameters));
            return Task.FromResult(1);
        }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(string sql, IReadOnlyDictionary<string, object?> parameters, MySqlDatabaseTarget databaseTarget, CancellationToken cancellationToken = default)
            => Task.FromResult(_rows);

        public Task<int> ExecuteSqlNonQueryAsync(string sql, IReadOnlyDictionary<string, object?> parameters, MySqlDatabaseTarget databaseTarget, CancellationToken cancellationToken = default)
            => Task.FromResult(0);
    }
}
