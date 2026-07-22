using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Contracts.Services;
using MTM_Waitlist.Core.Contracts.Services;
using MTM_Waitlist.Models;
using MTM_Waitlist.Services;

namespace MTM_Waitlist.Tests.Services;

[TestClass]
public sealed class StartupCoordinatorTests
{
    private const string RecoveryProbeKey = "Developer.RecoveryProbe";

    [TestMethod]
    public async Task RunAsync_ReturnsBlocked_WhenConfigurationPathsAreMissing()
    {
        var localSettingsService = new RecordingLocalSettingsService();
        var recoveryService = new StartupRecoveryService(localSettingsService);

        var coordinator = CreateCoordinator(
            new LocalSettingsOptions(),
            localSettingsService,
            recoveryService);

        var result = await coordinator.RunAsync();

        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.IsBlocked);
        Assert.AreEqual(string.Empty, result.RouteTarget);
        Assert.AreEqual(0, localSettingsService.ReadSettingCallCount);
        Assert.AreEqual(0, localSettingsService.ResetSettingCallCount);
    }

    [TestMethod]
    public async Task RunAsync_ReturnsSuccess_WhenProbeIsReadable()
    {
        var fileService = new InMemoryFileService(new Dictionary<string, object>
        {
            [RecoveryProbeKey] = "\"ok\""
        });

        var localSettingsService = CreateLocalSettingsService(fileService);
        var coordinator = CreateCoordinator(
            new LocalSettingsOptions
            {
                ApplicationDataFolder = "MTM_Waitlist/ApplicationData",
                LocalSettingsFile = "LocalSettings.json"
            },
            localSettingsService,
            new StartupRecoveryService(localSettingsService));

        var result = await coordinator.RunAsync();

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(result.IsBlocked);
        Assert.AreEqual(typeof(MTM_Waitlist.ViewModels.MainShellViewModel).FullName, result.RouteTarget);
        Assert.IsTrue(fileService.CurrentState.ContainsKey(RecoveryProbeKey));
    }

    [TestMethod]
    public async Task RunAsync_WhenProbeReadFails_RepairsSettingAndSucceeds()
    {
        var originalUserName = Environment.GetEnvironmentVariable("USERNAME");
        Environment.SetEnvironmentVariable("USERNAME", "Phase02TestUser");

        try
        {
            var fileService = new InMemoryFileService(new Dictionary<string, object>
            {
                [RecoveryProbeKey] = new object()
            });

            var localSettingsService = new LocalSettingsService(
                fileService,
                Options.Create(new LocalSettingsOptions
                {
                    ApplicationDataFolder = "MTM_Waitlist/ApplicationData",
                    LocalSettingsFile = "LocalSettings.json"
                }));

            var coordinator = new StartupCoordinator(
                Options.Create(new LocalSettingsOptions
                {
                    ApplicationDataFolder = "MTM_Waitlist/ApplicationData",
                    LocalSettingsFile = "LocalSettings.json"
                }),
                localSettingsService,
                new StartupRecoveryService(localSettingsService),
                new StartupState());

            var result = await coordinator.RunAsync();

            Assert.IsTrue(result.IsSuccess);
            Assert.IsFalse(result.IsBlocked);
            Assert.AreEqual(typeof(ViewModels.MainShellViewModel).FullName, result.RouteTarget);
            Assert.IsFalse(fileService.CurrentState.ContainsKey(RecoveryProbeKey));
        }
        finally
        {
            Environment.SetEnvironmentVariable("USERNAME", originalUserName);
        }
    }

    private static StartupCoordinator CreateCoordinator(
        LocalSettingsOptions settingsOptions,
        ILocalSettingsService localSettingsService,
        IStartupRecoveryService recoveryService)
    {
        return new StartupCoordinator(
            Options.Create(settingsOptions),
            localSettingsService,
            recoveryService,
            new StartupState());
    }

    private static LocalSettingsService CreateLocalSettingsService(InMemoryFileService fileService)
    {
        return new LocalSettingsService(
            fileService,
            Options.Create(new LocalSettingsOptions
            {
                ApplicationDataFolder = "MTM_Waitlist/ApplicationData",
                LocalSettingsFile = "LocalSettings.json"
            }));
    }

    private sealed class RecordingLocalSettingsService : ILocalSettingsService
    {
        public int ReadSettingCallCount { get; private set; }

        public int ResetSettingCallCount { get; private set; }

        public Task<T?> ReadSettingAsync<T>(string key)
        {
            ReadSettingCallCount++;
            return Task.FromResult(default(T));
        }

        public Task SaveSettingAsync<T>(string key, T value)
        {
            return Task.CompletedTask;
        }

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default)
        {
            ResetSettingCallCount++;
            return Task.CompletedTask;
        }

        public Task ResetAsync()
        {
            return Task.CompletedTask;
        }

        public Task CorruptForTestAsync()
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryFileService : IFileService
    {
        private Dictionary<string, object> _state;

        public InMemoryFileService(Dictionary<string, object> initialState)
        {
            _state = new Dictionary<string, object>(initialState);
        }

        public Dictionary<string, object> CurrentState => new(_state);

        public T? Read<T>(string folderPath, string fileName)
        {
            if (typeof(T) != typeof(IDictionary<string, object>))
            {
                return default;
            }

            return (T)(object)new Dictionary<string, object>(_state);
        }

        public void Save<T>(string folderPath, string fileName, T content)
        {
            if (content is IDictionary<string, object> dictionary)
            {
                _state = new Dictionary<string, object>(dictionary);
            }
        }

        public void Delete(string folderPath, string fileName)
        {
            _state.Clear();
        }
    }
}