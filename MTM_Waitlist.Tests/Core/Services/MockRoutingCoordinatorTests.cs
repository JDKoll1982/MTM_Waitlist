using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class MockRoutingCoordinatorTests
{
    [TestMethod]
    public async Task GetEffectiveDecisionAsync_Unreachable_ForcesMock_RegardlessOfSettings()
    {
        var health = new StubConnectionHealthService(ConnectionHealthStatus.Unreachable);
        var config = new StubMockConfigurationService(new MockSettingState { IsPresent = true, IsMockEnabled = false, SettingKey = "Feature.RecvMockData" });
        var local = new StubLocalSettingsService(false);
        var coordinator = new MockRoutingCoordinator(health, config, new MockRoutingService(), local);

        var decision = await coordinator.GetEffectiveDecisionAsync(ConnectionSource.Receiving);

        Assert.IsTrue(decision.UseMockData);
        Assert.IsTrue(decision.IsAutoForced);
    }

    [TestMethod]
    public async Task GetEffectiveDecisionAsync_NoHealthCheck_ReadsLocalOverrideKeyForSource()
    {
        var health = new StubConnectionHealthService(ConnectionHealthStatus.Connected);
        var config = new StubMockConfigurationService(new MockSettingState { IsPresent = false, SettingKey = "Feature.InforVisualMockData" });
        var local = new StubLocalSettingsService(null);
        var coordinator = new MockRoutingCoordinator(health, config, new MockRoutingService(), local);

        await coordinator.GetEffectiveDecisionAsync(ConnectionSource.InforVisual);

        Assert.AreEqual("Feature.InforVisualMockData", local.LastReadKey);
    }

    [TestMethod]
    public async Task GetEffectiveDecisionAsync_RefreshHealth_UsesFreshProbe()
    {
        var health = new StubConnectionHealthService(ConnectionHealthStatus.Connected);
        var config = new StubMockConfigurationService(new MockSettingState { IsPresent = false, SettingKey = "Feature.RecvMockData" });
        var local = new StubLocalSettingsService(true);
        var coordinator = new MockRoutingCoordinator(health, config, new MockRoutingService(), local);

        var decision = await coordinator.GetEffectiveDecisionAsync(ConnectionSource.Receiving, refreshHealth: true);

        Assert.IsTrue(health.CheckAsyncCalled);
        Assert.IsTrue(decision.UseMockData); // local override true wins over central-absent when reachable
    }

    [TestMethod]
    public async Task GetEffectiveDecisionsAsync_ReturnsDecisionForEverySource()
    {
        var health = new StubConnectionHealthService(ConnectionHealthStatus.Connected);
        var config = new StubMockConfigurationService(null);
        var local = new StubLocalSettingsService(null);
        var coordinator = new MockRoutingCoordinator(health, config, new MockRoutingService(), local);

        var decisions = await coordinator.GetEffectiveDecisionsAsync();

        Assert.AreEqual(2, decisions.Count);
        Assert.IsTrue(decisions.ContainsKey(ConnectionSource.InforVisual));
        Assert.IsTrue(decisions.ContainsKey(ConnectionSource.Receiving));
        Assert.IsFalse(decisions[ConnectionSource.InforVisual].UseMockData);
    }

    private sealed class StubConnectionHealthService : IConnectionHealthService
    {
        private readonly ConnectionHealthStatus _status;

        public StubConnectionHealthService(ConnectionHealthStatus status) => _status = status;

        public bool CheckAsyncCalled { get; private set; }

        public Task<ConnectionHealthState> CheckAsync(ConnectionSource source, CancellationToken cancellationToken = default)
        {
            CheckAsyncCalled = true;
            return Task.FromResult(new ConnectionHealthState { Source = source, Status = _status });
        }

        public Task<IReadOnlyDictionary<ConnectionSource, ConnectionHealthState>> CheckAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<ConnectionSource, ConnectionHealthState>>(
                new Dictionary<ConnectionSource, ConnectionHealthState>
                {
                    [ConnectionSource.InforVisual] = new() { Source = ConnectionSource.InforVisual, Status = _status },
                    [ConnectionSource.Receiving] = new() { Source = ConnectionSource.Receiving, Status = _status },
                });

        public ConnectionHealthState GetLastKnown(ConnectionSource source)
            => new() { Source = source, Status = _status };
    }

    private sealed class StubMockConfigurationService : IMockConfigurationService
    {
        private readonly MockSettingState? _state;

        public StubMockConfigurationService(MockSettingState? state) => _state = state;

        public Task<MockSettingState> GetMockSettingAsync(ConnectionSource source, CancellationToken cancellationToken = default)
        {
            var state = _state ?? new MockSettingState
            {
                SettingKey = MockSettingKeys.For(source),
                IsPresent = false,
                IsMockEnabled = false,
            };
            return Task.FromResult(state);
        }

        public Task<bool> SetMockSettingAsync(ConnectionSource source, bool enabled, long? updatedByUserId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class StubLocalSettingsService : ILocalSettingsService
    {
        private readonly bool? _value;

        public StubLocalSettingsService(bool? value) => _value = value;

        public string? LastReadKey { get; private set; }

        public Task<T?> ReadSettingAsync<T>(string key)
        {
            LastReadKey = key;
            var value = _value is null ? default : (T)(object)_value;
            return Task.FromResult(value);
        }

        public Task SaveSettingAsync<T>(string key, T value) => Task.CompletedTask;

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ResetAsync() => Task.CompletedTask;

        public Task CorruptForTestAsync() => Task.CompletedTask;
    }
}
