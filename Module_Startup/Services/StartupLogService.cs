using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

public sealed class StartupLogService : BackgroundService, IStartupLogService
{
    private readonly Channel<StartupLogEntry> _channel;
    private readonly StartupLoggingOptions _options;
    private readonly IStartupLogForwarder _forwarder;
    private readonly ILocalSettingsService _localSettingsService;
    private string _previousHash = string.Empty;

    public StartupLogService(
        IOptions<StartupLoggingOptions> options,
        IStartupLogForwarder forwarder,
        ILocalSettingsService localSettingsService)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(forwarder);
        ArgumentNullException.ThrowIfNull(localSettingsService);

        _options = options.Value;
        _forwarder = forwarder;
        _localSettingsService = localSettingsService;

        var capacity = Math.Max(128, _options.ChannelCapacity);
        _channel = Channel.CreateBounded<StartupLogEntry>(new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });
    }

    public void Info(string area, string message)
    {
        Enqueue("INFO", area, message, null);
    }

    public void Error(string area, Exception? exception, string message)
    {
        Enqueue("ERROR", area, message, exception?.ToString());
    }

    public async override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.TryComplete();
        await base.StopAsync(cancellationToken);
    }

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = RunRetentionCleanupSafeAsync(CancellationToken.None);

        try
        {
            // Drain until writer completion to avoid cancellation exceptions during normal shutdown.
            await foreach (var entry in _channel.Reader.ReadAllAsync(CancellationToken.None))
            {
                await ProcessEntrySafeAsync(entry, CancellationToken.None);
            }
        }
        catch (OperationCanceledException)
        {
            while (_channel.Reader.TryRead(out var pendingEntry))
            {
                await ProcessEntrySafeAsync(pendingEntry, CancellationToken.None);
            }
        }
    }

    private void Enqueue(string level, string area, string message, string? exception)
    {
        var entry = new StartupLogEntry(
            DateTimeOffset.UtcNow,
            string.IsNullOrWhiteSpace(level) ? "INFO" : level,
            string.IsNullOrWhiteSpace(area) ? "General" : area,
            message ?? string.Empty,
            exception);

        if (!_channel.Writer.TryWrite(entry))
        {
            Debug.WriteLine($"[Logging][{entry.TimestampUtc:O}] Dropped log event because logging channel is unavailable.");
        }
    }

    private async Task ProcessEntrySafeAsync(StartupLogEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            var payload = BuildPayload(entry);
            var line = JsonSerializer.Serialize(payload);

            await WriteToHostedVmLogAsync(entry.TimestampUtc, line, cancellationToken);
            await ForwardWithRetryAsync(line, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during host shutdown when background work is canceled.
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Logging][{DateTimeOffset.UtcNow:O}] Failed to persist startup log event: {ex}");
        }
    }

    private StartupLogPayload BuildPayload(StartupLogEntry entry)
    {
        var baseEvent = new StartupLogPayloadBase
        {
            TimestampUtc = entry.TimestampUtc,
            Level = entry.Level,
            Area = entry.Area,
            Message = entry.Message,
            Exception = entry.Exception,
            PreviousHash = _previousHash
        };

        var canonical = JsonSerializer.Serialize(baseEvent);
        var hashInput = string.Concat(canonical, "|", _previousHash);
        var currentHash = ComputeSha256(hashInput);

        _previousHash = currentHash;

        return new StartupLogPayload
        {
            TimestampUtc = baseEvent.TimestampUtc,
            Level = baseEvent.Level,
            Area = baseEvent.Area,
            Message = baseEvent.Message,
            Exception = baseEvent.Exception,
            PreviousHash = baseEvent.PreviousHash,
            Hash = currentHash
        };
    }

    private async Task WriteToHostedVmLogAsync(DateTimeOffset timestampUtc, string line, CancellationToken cancellationToken)
    {
        var configuredDirectory = await ResolveHostedVmLogDirectoryAsync();
        var directory = ResolvePath(configuredDirectory);
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, $"startup_daily_{timestampUtc:yyyy_MM_dd}.jsonl");
        await File.AppendAllTextAsync(filePath, line + Environment.NewLine, Encoding.UTF8, cancellationToken);
    }

    private async Task ForwardWithRetryAsync(string line, CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Max(1, _options.ForwardRetryCount + 1);
        var delay = TimeSpan.FromMilliseconds(150);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await _forwarder.ForwardAsync(line, cancellationToken);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Expected during shutdown.
                return;
            }
            catch when (attempt < maxAttempts)
            {
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
            }
        }
    }

    private async Task RunRetentionCleanupSafeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Yield();
            var configuredDirectory = await ResolveHostedVmLogDirectoryAsync();
            var directory = ResolvePath(configuredDirectory);
            if (!Directory.Exists(directory))
            {
                return;
            }

            var retentionCutoffUtc = DateTimeOffset.UtcNow.AddDays(-Math.Max(1, _options.RetentionDays));
            var files = new DirectoryInfo(directory)
                .GetFiles("startup_daily_*.jsonl", SearchOption.TopDirectoryOnly)
                .OrderBy(file => file.CreationTimeUtc)
                .ToList();

            foreach (var file in files.Where(file => file.LastWriteTimeUtc < retentionCutoffUtc.UtcDateTime).ToList())
            {
                cancellationToken.ThrowIfCancellationRequested();
                file.Delete();
            }

            files = new DirectoryInfo(directory)
                .GetFiles("startup_daily_*.jsonl", SearchOption.TopDirectoryOnly)
                .OrderBy(file => file.CreationTimeUtc)
                .ToList();

            var maxBytes = (long)Math.Max(1, _options.MaxDirectorySizeMb) * 1024L * 1024L;
            var currentBytes = files.Sum(file => file.Length);

            foreach (var file in files)
            {
                if (currentBytes <= maxBytes)
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();
                var length = file.Length;
                file.Delete();
                currentBytes -= length;
            }
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Logging][{DateTimeOffset.UtcNow:O}] Retention cleanup failed: {ex}");
        }
    }

    private async Task<string> ResolveHostedVmLogDirectoryAsync()
    {
        try
        {
            var localDirectory = await _localSettingsService.ReadSettingAsync<string>(StartupLoggingOptions.HostedVmLogDirectorySettingKey);
            if (!string.IsNullOrWhiteSpace(localDirectory))
            {
                return localDirectory.Trim();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Logging][{DateTimeOffset.UtcNow:O}] Failed to read hosted log directory override: {ex}");
        }

        return _options.HostedVmLogDirectory;
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string ResolvePath(string configuredPath)
    {
        var trimmed = configuredPath?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            trimmed = "MTM_Waitlist/Logs/Startup";
        }

        if (Path.IsPathRooted(trimmed))
        {
            return trimmed;
        }

        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(basePath, trimmed);
    }

    private sealed record StartupLogEntry(
        DateTimeOffset TimestampUtc,
        string Level,
        string Area,
        string Message,
        string? Exception);

    private class StartupLogPayloadBase
    {
        public DateTimeOffset TimestampUtc { get; init; }

        public string Level { get; init; } = string.Empty;

        public string Area { get; init; } = string.Empty;

        public string Message { get; init; } = string.Empty;

        public string? Exception { get; init; }

        public string PreviousHash { get; init; } = string.Empty;
    }

    private sealed class StartupLogPayload : StartupLogPayloadBase
    {
        public string Hash { get; init; } = string.Empty;
    }
}
