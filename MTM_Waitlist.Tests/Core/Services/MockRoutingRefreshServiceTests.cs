using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class MockRoutingRefreshServiceTests
{
    private const ConnectionSource Focus = ConnectionSource.Receiving;

    [TestMethod]
    public async Task FirstRefresh_EstablishesBaseline_NoChanges()
    {
        var env = CreateEnv(threshold: 1, reachable: true);
        var service = CreateService(env);

        var changes = await service.RefreshAsync();

        Assert.AreEqual(0, changes.Count); // baseline only
        Assert.IsTrue(service.LastDecisions.ContainsKey(Focus));
        Assert.IsFalse(service.LastDecisions[Focus].UseMockData);
    }

    [TestMethod]
    public async Task Outage_ForceForcesMockOn_EmitsForcedOn()
    {
        var env = CreateEnv(threshold: 1, reachable: true);
        var service = CreateService(env);
        await service.RefreshAsync(); // baseline live

        env.SetReachable(Focus, false);
        var changes = await service.RefreshAsync();

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(MockModeChangeKind.ForcedOn, changes[0].Kind);
        Assert.IsTrue(service.LastDecisions[Focus].UseMockData);
        Assert.IsTrue(service.LastDecisions[Focus].IsAutoForced);
    }

    [TestMethod]
    public async Task Recovery_AfterOutage_EmitsRecovered()
    {
        var env = CreateEnv(threshold: 1, reachable: true);
        var service = CreateService(env);
        await service.RefreshAsync(); // baseline live

        env.SetReachable(Focus, false);
        await service.RefreshAsync(); // forced on

        env.SetReachable(Focus, true);
        var changes = await service.RefreshAsync(); // recovered

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(MockModeChangeKind.Recovered, changes[0].Kind);
        Assert.IsFalse(service.LastDecisions[Focus].UseMockData);
        Assert.IsFalse(service.LastDecisions[Focus].IsAutoForced);
    }

    [TestMethod]
    public async Task Debounce_PreventsThrash_SingleBlipEmitsNoChange()
    {
        var env = CreateEnv(threshold: 2, reachable: true);
        var service = CreateService(env);
        await service.RefreshAsync(); // unknown -> connected? establish
        await service.RefreshAsync(); // connected stable

        // Single transient outage must NOT flip mock (debounced).
        env.SetReachable(Focus, false);
        var blipChanges = await service.RefreshAsync();
        Assert.AreEqual(0, blipChanges.Count);
        Assert.IsFalse(service.LastDecisions[Focus].IsAutoForced);

        // Persistent outage (threshold met) now forces mock on.
        var changes = await service.RefreshAsync();
        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(MockModeChangeKind.ForcedOn, changes[0].Kind);
    }

    [TestMethod]
    public async Task CentralConfigFlip_Reachable_EmitsTurnedOn()
    {
        var env = CreateEnv(threshold: 1, reachable: true);
        var service = CreateService(env);
        await service.RefreshAsync(); // baseline: central off -> live

        env.SetCentral(Focus, true);
        var changes = await service.RefreshAsync();

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(MockModeChangeKind.TurnedOn, changes[0].Kind);
        Assert.IsTrue(service.LastDecisions[Focus].UseMockData);
        Assert.IsFalse(service.LastDecisions[Focus].IsAutoForced);
    }

    private static TestEnv CreateEnv(int threshold, bool reachable)
    {
        var env = new TestEnv
        {
            Debouncer = new MockFallbackDebouncer(threshold),
            Health = new StubHealth(reachable),
            Config = new StubConfig(),
            Local = new StubLocalSettings(),
        };
        return env;
    }

    private static MockRoutingRefreshService CreateService(TestEnv env)
        => new(env.Health, env.Config, new MockRoutingService(), env.Local, env.Debouncer);

    private sealed class TestEnv
    {
        public MockFallbackDebouncer Debouncer { get; init; } = new();

        public StubHealth Health { get; init; } = new(true);

        public StubConfig Config { get; init; } = new();

        public StubLocalSettings Local { get; init; } = new();

        public void SetReachable(ConnectionSource source, bool reachable) => Health.Set(source, reachable);

        public void SetCentral(ConnectionSource source, bool enabled) => Config.Set(source, enabled);
    }

    private sealed class StubHealth : IConnectionHealthService
    {
        private readonly Dictionary<ConnectionSource, bool> _reachable = new();

        public StubHealth(bool reachable)
        {
            _reachable[ConnectionSource.InforVisual] = reachable;
            _reachable[ConnectionSource.Receiving] = reachable;
        }

        public void Set(ConnectionSource source, bool reachable) => _reachable[source] = reachable;

        public Task<ConnectionHealthState> CheckAsync(ConnectionSource source, CancellationToken cancellationToken = default)
            => Task.FromResult(new ConnectionHealthState
            {
                Source = source,
                Status = _reachable[source] ? ConnectionHealthStatus.Connected : ConnectionHealthStatus.Unreachable,
                CheckedUtc = DateTimeOffset.UtcNow,
            });

        public Task<IReadOnlyDictionary<ConnectionSource, ConnectionHealthState>> CheckAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<ConnectionSource, ConnectionHealthState>>(
                new Dictionary<ConnectionSource, ConnectionHealthState>
                {
                    [ConnectionSource.InforVisual] = new() { Source = ConnectionSource.InforVisual, Status = _reachable[ConnectionSource.InforVisual] ? ConnectionHealthStatus.Connected : ConnectionHealthStatus.Unreachable },
                    [ConnectionSource.Receiving] = new() { Source = ConnectionSource.Receiving, Status = _reachable[ConnectionSource.Receiving] ? ConnectionHealthStatus.Connected : ConnectionHealthStatus.Unreachable },
                });

        public ConnectionHealthState GetLastKnown(ConnectionSource source)
            => new() { Source = source, Status = _reachable[source] ? ConnectionHealthStatus.Connected : ConnectionHealthStatus.Unreachable };
    }

    private sealed class StubConfig : IMockConfigurationService
    {
        private readonly Dictionary<ConnectionSource, bool> _enabled = new();

        public StubConfig() => _enabled[ConnectionSource.Receiving] = false;

        public void Set(ConnectionSource source, bool enabled) => _enabled[source] = enabled;

        public Task<MockSettingState> GetMockSettingAsync(ConnectionSource source, CancellationToken cancellationToken = default)
        {
            _enabled.TryGetValue(source, out var enabled);
            return Task.FromResult(new MockSettingState { SettingKey = MockSettingKeys.For(source), IsPresent = true, IsMockEnabled = enabled });
        }

        public Task<bool> SetMockSettingAsync(ConnectionSource source, bool enabled, long? updatedByUserId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class StubLocalSettings : ILocalSettingsService
    {
        public Task<T?> ReadSettingAsync<T>(string key) => Task.FromResult<T?>(default);

        public Task SaveSettingAsync<T>(string key, T value) => Task.CompletedTask;

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ResetAsync() => Task.CompletedTask;

        public Task CorruptForTestAsync() => Task.CompletedTask;
    }
}
