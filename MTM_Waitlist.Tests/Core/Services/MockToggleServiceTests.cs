using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class MockToggleServiceTests
{
    [TestMethod]
    public async Task GetEffective_ReadsMasterKey_DefaultsOff()
    {
        var settings = new StubLocalSettings();
        var service = new MockToggleService(settings);

        Assert.IsFalse(await service.GetEffectiveAsync()); // absent -> off
        Assert.AreEqual("Feature.InforVisualMockData", settings.LastReadKey);
    }

    [TestMethod]
    public async Task GetEffective_ReturnsMasterValue()
    {
        var settings = new StubLocalSettings();
        settings.Set("Feature.InforVisualMockData", true);
        var service = new MockToggleService(settings);

        Assert.IsTrue(await service.GetEffectiveAsync());
    }

    [TestMethod]
    public async Task Set_WritesBothKeysToSameValue()
    {
        var settings = new StubLocalSettings();
        var service = new MockToggleService(settings);

        await service.SetAsync(true);

        Assert.IsTrue(settings.Get("Feature.InforVisualMockData") is bool a && a);
        Assert.IsTrue(settings.Get("Feature.RecvMockData") is bool b && b);
    }

    private sealed class StubLocalSettings : ILocalSettingsService
    {
        private readonly Dictionary<string, object?> _store = new();

        public string? LastReadKey { get; private set; }

        public void Set(string key, bool value) => _store[key] = value;

        public object? Get(string key) => _store.TryGetValue(key, out var v) ? v : null;

        public Task<T?> ReadSettingAsync<T>(string key)
        {
            LastReadKey = key;
            if (!_store.TryGetValue(key, out var raw) || raw is not T typed)
            {
                return Task.FromResult<T?>(default);
            }

            return Task.FromResult<T?>(typed);
        }

        public Task SaveSettingAsync<T>(string key, T value)
        {
            _store[key] = value;
            return Task.CompletedTask;
        }

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default)
        {
            _store.Remove(key);
            return Task.CompletedTask;
        }

        public Task ResetAsync() => Task.CompletedTask;

        public Task CorruptForTestAsync() => Task.CompletedTask;
    }
}
