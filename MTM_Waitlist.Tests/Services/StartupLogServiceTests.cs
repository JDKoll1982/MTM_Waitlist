using System.Text.Json;

using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Services;

[TestClass]
public sealed class StartupLogServiceTests
{
    [TestMethod]
    public async Task LogService_WritesJsonlAndChainsHashesAsync()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "MTM_Waitlist.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootPath);

        var configuredPath = Path.Combine(rootPath, "configured-fallback");
        var options = Options.Create(new StartupLoggingOptions
        {
            HostedVmLogDirectory = configuredPath,
            CentralizedDestination = string.Empty,
            RetentionDays = 14,
            MaxDirectorySizeMb = 250,
            ChannelCapacity = 256,
            ForwardRetryCount = 0
        });

        var localSettings = new InMemoryLocalSettingsService(new Dictionary<string, object>
        {
            [StartupLoggingOptions.HostedVmLogDirectorySettingKey] = rootPath
        });

        var forwarder = new RecordingForwarder();
        var service = new StartupLogService(options, forwarder, localSettings);

        await service.StartAsync(CancellationToken.None);
        service.Info("Test", "first");
        service.Error("Test", new InvalidOperationException("boom"), "second");
        await Task.Delay(250);
        await service.StopAsync(CancellationToken.None);

        var logFile = Directory
            .GetFiles(rootPath, "startup_daily_*.jsonl", SearchOption.TopDirectoryOnly)
            .Single();

        var lines = File.ReadAllLines(logFile)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        Assert.AreEqual(2, lines.Length);
        Assert.AreEqual(2, forwarder.ForwardedLines.Count);

        using var firstDocument = JsonDocument.Parse(lines[0]);
        using var secondDocument = JsonDocument.Parse(lines[1]);

        var firstHash = firstDocument.RootElement.GetProperty("Hash").GetString();
        var secondPreviousHash = secondDocument.RootElement.GetProperty("PreviousHash").GetString();

        Assert.IsFalse(string.IsNullOrWhiteSpace(firstHash));
        Assert.AreEqual(firstHash, secondPreviousHash);
    }

    [TestMethod]
    public async Task LogService_CleanupDeletesExpiredFilesAsync()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "MTM_Waitlist.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootPath);

        var expiredFile = Path.Combine(rootPath, "startup_daily_2000_01_01.jsonl");
        await File.WriteAllTextAsync(expiredFile, "{}\n");
        File.SetLastWriteTimeUtc(expiredFile, DateTime.UtcNow.AddDays(-40));

        var options = Options.Create(new StartupLoggingOptions
        {
            HostedVmLogDirectory = rootPath,
            CentralizedDestination = string.Empty,
            RetentionDays = 14,
            MaxDirectorySizeMb = 250,
            ChannelCapacity = 128,
            ForwardRetryCount = 0
        });

        var service = new StartupLogService(options, new RecordingForwarder(), new InMemoryLocalSettingsService());

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(250);
        await service.StopAsync(CancellationToken.None);

        Assert.IsFalse(File.Exists(expiredFile));
    }

    private sealed class RecordingForwarder : IStartupLogForwarder
    {
        public List<string> ForwardedLines { get; } = new();

        public Task ForwardAsync(string jsonLine, CancellationToken cancellationToken = default)
        {
            ForwardedLines.Add(jsonLine);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryLocalSettingsService : ILocalSettingsService
    {
        private readonly Dictionary<string, object> _settings;

        public InMemoryLocalSettingsService()
            : this(new Dictionary<string, object>())
        {
        }

        public InMemoryLocalSettingsService(Dictionary<string, object> settings)
        {
            _settings = settings;
        }

        public Task<T?> ReadSettingAsync<T>(string key)
        {
            if (_settings.TryGetValue(key, out var value) && value is T typedValue)
            {
                return Task.FromResult<T?>(typedValue);
            }

            return Task.FromResult<T?>(default);
        }

        public Task SaveSettingAsync<T>(string key, T value)
        {
            if (value is null)
            {
                _settings.Remove(key);
            }
            else
            {
                _settings[key] = value;
            }

            return Task.CompletedTask;
        }

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _settings.Remove(key);
            return Task.CompletedTask;
        }

        public Task ResetAsync()
        {
            _settings.Clear();
            return Task.CompletedTask;
        }

        public Task CorruptForTestAsync()
        {
            throw new NotSupportedException();
        }
    }
}
