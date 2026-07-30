using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Services;

[TestClass]
public sealed class StartupRecoveryServiceTests
{
    [TestMethod]
    public async Task ResetSettingAsync_ForwardsToLocalSettingsServiceAsync()
    {
        var localSettingsService = new RecordingLocalSettingsService();
        var service = new StartupRecoveryService(localSettingsService);

        await service.ResetSettingAsync("Developer.RecoveryProbe");

        Assert.AreEqual("Developer.RecoveryProbe", localSettingsService.LastResetSettingKey);
        Assert.AreEqual(1, localSettingsService.ResetSettingCallCount);
    }

    [TestMethod]
    public async Task ResetToDefaultsAsync_ForwardsToLocalSettingsServiceAsync()
    {
        var localSettingsService = new RecordingLocalSettingsService();
        var service = new StartupRecoveryService(localSettingsService);

        await service.ResetToDefaultsAsync();

        Assert.AreEqual(1, localSettingsService.ResetCallCount);
    }

    private sealed class RecordingLocalSettingsService : ILocalSettingsService
    {
        public string? LastResetSettingKey { get; private set; }

        public int ResetSettingCallCount { get; private set; }

        public int ResetCallCount { get; private set; }

        public Task<T?> ReadSettingAsync<T>(string key)
        {
            return Task.FromResult(default(T));
        }

        public Task SaveSettingAsync<T>(string key, T value)
        {
            return Task.CompletedTask;
        }

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default)
        {
            LastResetSettingKey = key;
            ResetSettingCallCount++;
            return Task.CompletedTask;
        }

        public Task ResetAsync()
        {
            ResetCallCount++;
            return Task.CompletedTask;
        }

        public Task CorruptForTestAsync()
        {
            return Task.CompletedTask;
        }
    }
}