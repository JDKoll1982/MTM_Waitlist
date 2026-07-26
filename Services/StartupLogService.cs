using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using MTM_Waitlist.Contracts.Services;
using MTM_Waitlist.Models;

namespace MTM_Waitlist.Services;

public sealed class StartupLogService : BackgroundService, IStartupLogService
{
    private readonly Channel<StartupLogEntry> _channel;
    private readonly StartupLoggingOptions _options;
    private readonly IStartupLogForwarder _forwarder;
    private string _previousHash = string.Empty;

    public StartupLogService(IOptions<StartupLoggingOptions> options, IStartupLogForwarder forwarder)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(forwarder);

        _options = options.Value;
        _forwarder = forwarder;

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
        _ = RunRetentionCleanupSafeAsync(stoppingToken);

        try
        {
            await foreach (var entry in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                await ProcessEntrySafeAsync(entry, stoppingToken);
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
            Debug.WriteLine($"[STARTUP][{entry.TimestampUtc:O}][Logging] Dropped log event because channel is full.");
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
        catch (Exception ex)
        {
            Debug.WriteLine($"[STARTUP][{DateTimeOffset.UtcNow:O}][Logging] Failed to persist startup log event: {ex}");
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
        var directory = ResolvePath(_options.HostedVmLogDirectory);
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
            var directory = ResolvePath(_options.HostedVmLogDirectory);
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
            Debug.WriteLine($"[STARTUP][{DateTimeOffset.UtcNow:O}][Logging] Retention cleanup failed: {ex}");
        }
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
