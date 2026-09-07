using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class NewRequestAlertServiceTests
{
    [TestMethod]
    public async Task GetEnabled_DefaultsOff_WhenNeverSet()
    {
        var settings = new StubLocalSettings();
        var service = new NewRequestAlertService(settings);

        Assert.IsFalse(await service.GetEnabledAsync());
        Assert.AreEqual(NewRequestAlertService.SettingKeyName, settings.LastReadKey);
    }

    [TestMethod]
    public async Task GetEnabled_ReturnsStoredValue()
    {
        var settings = new StubLocalSettings();
        settings.Set(NewRequestAlertService.SettingKeyName, true);
        var service = new NewRequestAlertService(settings);

        Assert.IsTrue(await service.GetEnabledAsync());
    }

    [TestMethod]
    public async Task Set_PersistsUnderSettingKey()
    {
        var settings = new StubLocalSettings();
        var service = new NewRequestAlertService(settings);

        await service.SetEnabledAsync(true);

        Assert.IsTrue(settings.Get(NewRequestAlertService.SettingKeyName) is bool value && value);
    }

    [TestMethod]
    public async Task ShouldNotifyOnCreated_RequiresSignalEnabledAndPackaged()
    {
        var settings = new StubLocalSettings();
        settings.Set(NewRequestAlertService.SettingKeyName, true);
        var service = new NewRequestAlertService(settings);

        Assert.IsTrue(await service.ShouldNotifyOnCreatedAsync(requestCreatedSignal: true, isPackaged: true));
        Assert.IsFalse(await service.ShouldNotifyOnCreatedAsync(requestCreatedSignal: false, isPackaged: true));
        Assert.IsFalse(await service.ShouldNotifyOnCreatedAsync(requestCreatedSignal: true, isPackaged: false));
    }

    [TestMethod]
    public async Task ShouldNotifyOnCreated_RespectsOffToggle()
    {
        // Never set -> defaults OFF, so even when a request is created in a packaged app, no toast.
        var settings = new StubLocalSettings();
        var service = new NewRequestAlertService(settings);

        Assert.IsFalse(await service.ShouldNotifyOnCreatedAsync(requestCreatedSignal: true, isPackaged: true));
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
