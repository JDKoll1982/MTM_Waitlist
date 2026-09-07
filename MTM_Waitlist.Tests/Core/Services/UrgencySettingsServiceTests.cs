using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class UrgencySettingsServiceTests
{
    [TestMethod]
    public async Task GetMaxAllotted_DefaultsTo30Minutes_WhenUnset()
    {
        var settings = new StubLocalSettings();
        var service = new UrgencySettingsService(settings);

        var result = await service.GetMaxAllottedAsync("Pickup Coil");

        Assert.AreEqual(TimeSpan.FromMinutes(30), result);
        Assert.AreEqual(TimeSpan.FromMinutes(UrgencySettingsService.DefaultMinutes), service.DefaultMaxAllotted);
    }

    [TestMethod]
    public async Task SetThenGet_RoundTripsPerSubtype()
    {
        var settings = new StubLocalSettings();
        var service = new UrgencySettingsService(settings);

        await service.SetMaxAllottedAsync("Pickup Coil", 45);
        await service.SetMaxAllottedAsync("Wrong Coil", 20);

        Assert.AreEqual(TimeSpan.FromMinutes(45), await service.GetMaxAllottedAsync("Pickup Coil"));
        Assert.AreEqual(TimeSpan.FromMinutes(20), await service.GetMaxAllottedAsync("Wrong Coil"));
        // Values are stored per-subtype under separate keys.
        Assert.IsTrue(settings.Has(UrgencySettingsService.KeyPrefix + "Pickup Coil"));
        Assert.IsTrue(settings.Has(UrgencySettingsService.KeyPrefix + "Wrong Coil"));
    }

    [TestMethod]
    public async Task Set_ClampsToPositiveMinimum()
    {
        var settings = new StubLocalSettings();
        var service = new UrgencySettingsService(settings);

        await service.SetMaxAllottedAsync("Pickup Coil", 0);
        Assert.AreEqual(TimeSpan.FromMinutes(1), await service.GetMaxAllottedAsync("Pickup Coil"));
    }

    [TestMethod]
    public async Task UnconfiguredSubtype_FallsBackToDefault()
    {
        var settings = new StubLocalSettings();
        var service = new UrgencySettingsService(settings);

        await service.SetMaxAllottedAsync("Pickup Coil", 45);

        // A different, never-configured subtype still uses the default.
        Assert.AreEqual(TimeSpan.FromMinutes(45), await service.GetMaxAllottedAsync("Pickup Coil"));
        Assert.AreEqual(TimeSpan.FromMinutes(30), await service.GetMaxAllottedAsync("Forklift Assist"));
    }

    private sealed class StubLocalSettings : ILocalSettingsService
    {
        private readonly Dictionary<string, object?> _store = new();

        public bool Has(string key) => _store.ContainsKey(key);

        public Task<T?> ReadSettingAsync<T>(string key)
        {
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
