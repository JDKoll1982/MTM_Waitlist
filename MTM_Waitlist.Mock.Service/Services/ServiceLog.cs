using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// The service's durable log file: one JSON-Lines file per UTC day under the service's app-data root.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists (T148(c)).</b> Every failure path in the shipped service wrote through
/// <see cref="MTM_Waitlist.Module_Core.Helpers.StartupDebugLog"/>, which is marked
/// <see cref="ConditionalAttribute"/>(<c>"DEBUG"</c>) — so a published <c>Release</c> build compiled the
/// calls out entirely — and which only reached <see cref="Debug.WriteLine"/>. Nothing on the host recorded
/// why a refresh cycle failed, and the two surfaces that would have reported it (<c>GET /api/status</c> and
/// the tray's Status page) are exactly the ones a hidden tray icon and a write-only credential put out of
/// reach. A service that cannot be asked why it is not working is not operable.
/// </para>
/// <para>
/// <b>Best-effort, never fatal.</b> Logging must not be able to stop the engines, so every failure here is
/// swallowed after a <see cref="Debug.WriteLine"/> trace. The file is appended to share-nothing style under
/// a lock, which is sufficient for a single tray process.
/// </para>
/// <para>
/// <b>No secret may be written here.</b> Callers pass message text only; the API's authentication path and
/// the refresh engine already redact credential material before it reaches a logger (FR-026).
/// </para>
/// </remarks>
internal static class ServiceLog
{
    /// <summary>Directory name under the service's app-data root that holds the daily log files.</summary>
    internal const string DirectoryName = "Logs";

    /// <summary>Prefix of each daily log file; the date is appended as <c>yyyy_MM_dd</c>.</summary>
    internal const string FileNamePrefix = "service_daily_";

    /// <summary>Number of days of log files kept before the oldest are deleted.</summary>
    internal const int RetentionDays = 30;

    private static readonly object s_gate = new();

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = false
    };

    private static string? s_directory;
    private static DateOnly? s_lastRetentionDay;

    /// <summary>The directory the daily files are written to.</summary>
    /// <remarks>
    /// Defaults to <c>%LOCALAPPDATA%\MTM_Waitlist.Mock.Service\Logs</c>, beside the configuration, run
    /// records and backups — so it survives a redeploy of the install folder, exactly like the rest of the
    /// service's own state.
    /// </remarks>
    internal static string Directory =>
        s_directory ?? Path.Combine(ServiceHostBuilder.GetDefaultAppDataRoot(), DirectoryName);

    /// <summary>
    /// Points the log at a directory, for tests and for a host that relocated its app-data root.
    /// </summary>
    /// <param name="directory">The directory to write to, or <see langword="null"/> to restore the default.</param>
    internal static void Configure(string? directory)
    {
        lock (s_gate)
        {
            s_directory = string.IsNullOrWhiteSpace(directory) ? null : directory.Trim();
            s_lastRetentionDay = null;
        }
    }

    /// <summary>Records an informational event.</summary>
    /// <param name="area">The source that owns the event, e.g. <c>ServiceApp</c>.</param>
    /// <param name="message">The message text. Must not contain a secret.</param>
    internal static void Info(string area, string message) => Write("INFO", area, message, exception: null);

    /// <summary>Records an error, with the exception's full text so a stack is preserved.</summary>
    /// <param name="area">The source that owns the event.</param>
    /// <param name="exception">The failure, or <see langword="null"/> when none is available.</param>
    /// <param name="message">The message text. Must not contain a secret.</param>
    internal static void Error(string area, Exception? exception, string message) =>
        Write("ERROR", area, message, exception?.ToString());

    /// <summary>
    /// Appends one JSON line to the current day's file.
    /// </summary>
    /// <param name="level">The level, e.g. <c>INFO</c> or <c>ERROR</c>.</param>
    /// <param name="area">The source that owns the event.</param>
    /// <param name="message">The message text.</param>
    /// <param name="exception">The exception text, when there is one.</param>
    internal static void Write(string level, string area, string message, string? exception)
    {
        var timestampUtc = DateTimeOffset.UtcNow;

        try
        {
            var line = JsonSerializer.Serialize(
                new ServiceLogEntry(
                    timestampUtc,
                    string.IsNullOrWhiteSpace(level) ? "INFO" : level,
                    string.IsNullOrWhiteSpace(area) ? "General" : area,
                    message ?? string.Empty,
                    exception),
                s_jsonOptions);

            lock (s_gate)
            {
                var directory = Directory;
                System.IO.Directory.CreateDirectory(directory);

                var filePath = Path.Combine(
                    directory,
                    $"{FileNamePrefix}{timestampUtc:yyyy_MM_dd}.jsonl");

                File.AppendAllText(filePath, line + Environment.NewLine, Encoding.UTF8);

                RunRetentionSweep(directory, DateOnly.FromDateTime(timestampUtc.UtcDateTime));
            }
        }
        catch (Exception ex)
        {
            // Logging must never be able to stop the engines, so this is traced and dropped.
            Debug.WriteLine($"[ServiceLog][{timestampUtc:O}] Failed to append: {ex}");
        }
    }

    /// <summary>
    /// Deletes whole days of log files older than the retention window, once per day.
    /// </summary>
    /// <param name="directory">The directory being written to.</param>
    /// <param name="todayUtc">The UTC day being written.</param>
    private static void RunRetentionSweep(string directory, DateOnly todayUtc)
    {
        if (s_lastRetentionDay == todayUtc)
        {
            return;
        }

        s_lastRetentionDay = todayUtc;

        var cutoff = todayUtc.AddDays(-RetentionDays);

        foreach (var file in new DirectoryInfo(directory).GetFiles($"{FileNamePrefix}*.jsonl"))
        {
            // The file name carries the day, so retention does not depend on the filesystem's timestamps.
            var stamp = file.Name[FileNamePrefix.Length..];
            if (stamp.Length < 10
                || !DateOnly.TryParseExact(
                    stamp[..10],
                    "yyyy_MM_dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var day))
            {
                continue;
            }

            if (day < cutoff)
            {
                try
                {
                    file.Delete();
                }
                catch (IOException)
                {
                    // Held open by a viewer; it will be deleted on a later sweep.
                }
            }
        }
    }

    /// <summary>One durable log line.</summary>
    private sealed record ServiceLogEntry(
        DateTimeOffset TimestampUtc,
        string Level,
        string Area,
        string Message,
        string? Exception);
}
