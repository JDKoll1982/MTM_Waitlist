using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Services;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.Services;

namespace MTM_Waitlist.Tests.Module_Setup.Services;

[TestClass]
public sealed class SetupPersistenceServiceTests
{
    [TestMethod]
    public async Task SaveAsync_WhenMockEnabled_ReturnsMockSavedMessageAndSkipsRegisterAsync()
    {
        var activeJobCoordinator = new FakeActiveJobCoordinatorService(hasActiveJob: false);
        var settings = new InMemoryLocalSettingsService(new Dictionary<string, object>
        {
            ["Feature.RecvMockData"] = true
        });
        var sampleDataService = new SampleDataService(settings);
        var mySqlHelperServer = new MySqlHelperServer(settings, sampleDataService);
        var service = new SetupPersistenceService(activeJobCoordinator, mySqlHelperServer);

        var request = CreateRequest();
        var result = await service.SaveAsync(request, false);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.RequiresReplacementConfirmation);
        Assert.IsTrue(result.Message.Contains("Mock", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(0, activeJobCoordinator.RegisterCalls);
    }

    [TestMethod]
    public async Task SaveAsync_WhenMockDisabled_AndBackendWritesNoRows_ReturnsFailureAsync()
    {
        var activeJobCoordinator = new FakeActiveJobCoordinatorService(hasActiveJob: false);
        var settings = new InMemoryLocalSettingsService(new Dictionary<string, object>
        {
            ["Feature.RecvMockData"] = false
        });
        var sampleDataService = new SampleDataService(settings);
        var mySqlHelperServer = new MySqlHelperServer(settings, sampleDataService);
        var service = new SetupPersistenceService(activeJobCoordinator, mySqlHelperServer);

        var request = CreateRequest();
        var result = await service.SaveAsync(request, false);

        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.RequiresReplacementConfirmation);
        Assert.IsTrue(result.Message.Contains("no rows", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(0, activeJobCoordinator.RegisterCalls);
    }

    [TestMethod]
    public async Task SaveAsync_WhenActiveJobExistsWithoutForce_ReturnsReplacementPromptAsync()
    {
        var activeJobCoordinator = new FakeActiveJobCoordinatorService(hasActiveJob: true);
        var settings = new InMemoryLocalSettingsService(new Dictionary<string, object>
        {
            ["Feature.RecvMockData"] = false
        });
        var sampleDataService = new SampleDataService(settings);
        var mySqlHelperServer = new MySqlHelperServer(settings, sampleDataService);
        var service = new SetupPersistenceService(activeJobCoordinator, mySqlHelperServer);

        var request = CreateRequest();
        var result = await service.SaveAsync(request, false);

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.RequiresReplacementConfirmation);
        Assert.AreEqual(0, activeJobCoordinator.RegisterCalls);
    }

    private static SetupSaveRequest CreateRequest()
    {
        return new SetupSaveRequest
        {
            WorkOrder = "WO-076951",
            PartNumber = "12345679",
            SequenceNumber = "20",
            WorkCenter = "Press 12"
        };
    }

    private sealed class FakeActiveJobCoordinatorService : IActiveJobCoordinatorService
    {
        private readonly bool _hasActiveJob;

        public FakeActiveJobCoordinatorService(bool hasActiveJob)
        {
            _hasActiveJob = hasActiveJob;
        }

        public int RegisterCalls { get; private set; }

        public Task<bool> HasActiveJobAsync(string workCenter, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_hasActiveJob);
        }

        public Task RegisterActiveJobAsync(SetupSaveRequest request, CancellationToken cancellationToken = default)
        {
            RegisterCalls++;
            return Task.CompletedTask;
        }
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
