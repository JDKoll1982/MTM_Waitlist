using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Services;
using MTM_Waitlist.Module_Setup.Services;

namespace MTM_Waitlist.Tests.Module_Setup.Services;

[TestClass]
public sealed class SetupDunnageWorkflowServiceTests
{
    [TestMethod]
    public async Task AddDunnageTypeAsync_WhenRoleIsNotAllowed_ReturnsFailure()
    {
        var service = CreateService();

        var result = await service.AddDunnageTypeAsync("TestType", "Operator");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message.Contains("permission", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task AddDunnagePartAsync_WhenRoleIsNotAllowed_ReturnsFailure()
    {
        var service = CreateService();

        var result = await service.AddDunnagePartAsync("1", "TestPart", "Operator");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message.Contains("permission", StringComparison.OrdinalIgnoreCase));
    }

    private static DunnageWorkflowService CreateService()
    {
        var settings = new InMemoryLocalSettingsService(new Dictionary<string, object>
        {
            ["Feature.RecvMockData"] = false,
            ["Feature.InforVisualMockData"] = true,
        });
        var sampleDataService = new SampleDataService(settings);
        var mySqlHelperServer = new MySqlHelperServer(settings, sampleDataService);
        return new DunnageWorkflowService(mySqlHelperServer);
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
