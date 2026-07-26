using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Core.Contracts.Services;
using MTM_Waitlist.Models;
using MTM_Waitlist.Services;

namespace MTM_Waitlist.Tests.Services;

[TestClass]
public sealed class LocalSettingsServiceTests
{
    private const string RecoveryProbeKey = "Developer.RecoveryProbe";

    [TestMethod]
    public async Task SaveSettingAsync_ThenReadSettingAsync_RoundTripsValueAsync()
    {
        var fileService = new InMemoryFileService();
        var service = CreateService(fileService);

        await service.SaveSettingAsync("Theme", "Dark");

        var theme = await service.ReadSettingAsync<string>("Theme");

        Assert.AreEqual("Dark", theme);
        Assert.IsTrue(fileService.CurrentState.ContainsKey("Theme"));
    }

    [TestMethod]
    public async Task ReadSettingAsync_ReturnsDefault_WhenSettingIsMissingAsync()
    {
        var service = CreateService(new InMemoryFileService());

        var value = await service.ReadSettingAsync<string>("Missing");

        Assert.IsNull(value);
    }

    [TestMethod]
    public async Task ResetSettingAsync_RemovesOnlyRequestedSettingAsync()
    {
        var fileService = new InMemoryFileService(new Dictionary<string, object>
        {
            [RecoveryProbeKey] = "\"corrupt\"",
            ["Keep.Me"] = "\"value\""
        });

        var service = CreateService(fileService);

        await service.ResetSettingAsync(RecoveryProbeKey);

        Assert.IsFalse(fileService.CurrentState.ContainsKey(RecoveryProbeKey));
        Assert.IsTrue(fileService.CurrentState.ContainsKey("Keep.Me"));
    }

    [TestMethod]
    public async Task ResetAsync_ClearsAllSettingsAsync()
    {
        var fileService = new InMemoryFileService(new Dictionary<string, object>
        {
            [RecoveryProbeKey] = "\"corrupt\"",
            ["Keep.Me"] = "\"value\""
        });

        var service = CreateService(fileService);

        await service.ResetAsync();

        Assert.AreEqual(0, fileService.CurrentState.Count);
    }

    [TestMethod]
    public async Task CorruptForTestAsync_WritesInvalidJsonPayloadAsync()
    {
        var appDataFolder = $"MTM_Waitlist/Tests/{Guid.NewGuid():N}";
        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appDataFolder, "LocalSettings.json");

        var service = new LocalSettingsService(
            new InMemoryFileService(),
            Options.Create(new LocalSettingsOptions
            {
                ApplicationDataFolder = appDataFolder,
                LocalSettingsFile = "LocalSettings.json"
            }));

        try
        {
            await service.CorruptForTestAsync();

            Assert.IsTrue(File.Exists(filePath));
            Assert.AreEqual("{ invalid-json", await File.ReadAllTextAsync(filePath));
        }
        finally
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    private static LocalSettingsService CreateService(InMemoryFileService fileService)
    {
        return new LocalSettingsService(
            fileService,
            Options.Create(new LocalSettingsOptions
            {
                ApplicationDataFolder = "MTM_Waitlist/ApplicationData",
                LocalSettingsFile = "LocalSettings.json"
            }));
    }

    private sealed class InMemoryFileService : IFileService
    {
        private Dictionary<string, object> _state;

        public InMemoryFileService()
        {
            _state = new Dictionary<string, object>();
        }

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