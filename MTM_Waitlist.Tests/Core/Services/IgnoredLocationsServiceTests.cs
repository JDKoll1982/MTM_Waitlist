using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class IgnoredLocationsServiceTests
{
    [TestMethod]
    public async Task GetIgnoredLocationsAsync_WhenUnset_ReturnsDefaults()
    {
        var service = new IgnoredLocationsService(new InMemorySettings());

        var result = await service.GetIgnoredLocationsAsync();

        CollectionAssert.AreEquivalent(new[] { "NCM", "NCM-VITS", "SHIP", "V-WC", "WC" }, result.ToArray());
    }

    [TestMethod]
    public async Task GetIgnoredLocationsAsync_ReturnsStoredAndNormalized()
    {
        var settings = new InMemorySettings();
        settings.SaveSettingAsync(IgnoredLocationDefaults.SettingKey, new List<string> { "wc", "SHIP ", "scrap-1", "wc" }).GetAwaiter().GetResult();
        var service = new IgnoredLocationsService(settings);

        var result = await service.GetIgnoredLocationsAsync();

        CollectionAssert.AreEquivalent(new[] { "SHIP", "SCRAP-1", "WC" }, result.ToArray());
    }

    [TestMethod]
    public async Task IsLocationIgnoredAsync_IsCaseInsensitiveAgainstDefaults()
    {
        var service = new IgnoredLocationsService(new InMemorySettings());

        Assert.IsTrue(await service.IsLocationIgnoredAsync("wc"));
        Assert.IsTrue(await service.IsLocationIgnoredAsync("NCM"));
        Assert.IsTrue(await service.IsLocationIgnoredAsync("v-wc"));
        Assert.IsFalse(await service.IsLocationIgnoredAsync("Rack A1"));
        Assert.IsFalse(await service.IsLocationIgnoredAsync(string.Empty));
    }

    [TestMethod]
    public async Task IsLocationIgnoredAsync_HonorsStoredSet()
    {
        var settings = new InMemorySettings();
        settings.SaveSettingAsync(IgnoredLocationDefaults.SettingKey, new List<string> { "SCRAP-1" }).GetAwaiter().GetResult();
        var service = new IgnoredLocationsService(settings);

        Assert.IsTrue(await service.IsLocationIgnoredAsync("scrap-1"));
        Assert.IsFalse(await service.IsLocationIgnoredAsync("WC"), "Defaults no longer apply when a stored set exists.");
    }

    private sealed class InMemorySettings : ILocalSettingsService
    {
        private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);

        public Task<T?> ReadSettingAsync<T>(string key)
        {
            if (_values.TryGetValue(key, out var value) && value is T typed)
            {
                return Task.FromResult<T?>(typed);
            }

            return Task.FromResult(default(T));
        }

        public Task SaveSettingAsync<T>(string key, T value)
        {
            _values[key] = value!;
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
