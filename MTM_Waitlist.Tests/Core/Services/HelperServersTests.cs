using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class HelperServersTests
{
    [TestMethod]
    public async Task SqlHelperServer_RoutesSearchToMockDataWhenEnabledAsync()
    {
        var settings = new InMemoryLocalSettingsService(new Dictionary<string, object>
        {
            ["Feature.UseMockData"] = true,
        });
        var sampleDataService = new SampleDataService(settings);
        var server = new SqlHelperServer(settings, sampleDataService);

        var result = await server.ExecuteReadOnlyQueueAsync("Waitlist.Search", "test");

        Assert.AreEqual(3, result.Count);
        Assert.IsTrue(result.OfType<MTM_Waitlist.Module_Waitlist.Models.SampleOrder>().Any(item => item.Title == "Coil Request"));
    }

    [TestMethod]
    public async Task MySqlHelperServer_RoutesLoadToMockDataWhenEnabledAsync()
    {
        var settings = new InMemoryLocalSettingsService(new Dictionary<string, object>
        {
            ["Feature.UseMockData"] = true,
        });
        var sampleDataService = new SampleDataService(settings);
        var server = new MySqlHelperServer(settings, sampleDataService);

        var result = await server.ExecuteReadWriteAsync("Waitlist.LoadOrders", "Vits Drive");

        Assert.AreEqual(3, result.Count);
        Assert.IsTrue(result.OfType<MTM_Waitlist.Module_Waitlist.Models.SampleOrder>().Any(item => item.Title == "Finished Goods Pickup"));
    }

    private sealed class InMemoryLocalSettingsService : ILocalSettingsService
    {
        private readonly Dictionary<string, object> _settings;

        public InMemoryLocalSettingsService(Dictionary<string, object> settings)
        {
            _settings = settings;
        }

        public Task<T?> ReadSettingAsync<T>(string key)
        {
            if (_settings.TryGetValue(key, out var value))
            {
                return Task.FromResult((T?)value);
            }

            return Task.FromResult(default(T));
        }

        public Task SaveSettingAsync<T>(string key, T value)
        {
            _settings[key] = value!;
            return Task.CompletedTask;
        }

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default)
        {
            _settings.Remove(key);
            return Task.CompletedTask;
        }

        public Task ResetAsync()
        {
            _settings.Clear();
            return Task.CompletedTask;
        }

        public Task CorruptForTestAsync() => Task.CompletedTask;
    }
}
