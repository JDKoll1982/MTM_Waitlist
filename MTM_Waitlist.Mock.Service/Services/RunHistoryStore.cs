using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// The service's append-only run history: one JSON line per refresh outcome and per backup outcome, kept
/// long enough that SC-007 and SC-008 can actually be measured against it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a second store.</b> <see cref="RefreshRunRecordStore"/> holds only the <i>latest</i> run per shape and
/// <see cref="BackupArtifactStore"/> only the latest run per store. That answers "what is the state of things
/// now", which is what the status surface needs, and it can never answer "what happened over the last thirty
/// days" — each new run overwrites the evidence for the one before it. The reliability criteria are statements
/// about a period, so they need a file that only grows.
/// </para>
/// <para>
/// <b>Service-local, never MySQL.</b> This is the same reason <see cref="RefreshRunRecord"/> is stored on disk
/// (data-model.md §4/§6): a restore replaces an entire store, so history kept inside one would be rewound by
/// the very operation it has to describe. It would also be unavailable exactly when it is most needed — while
/// the store is being restored.
/// </para>
/// <para>
/// <b>Append-only, with expiry.</b> A record is written once and never edited. The one thing that removes lines
/// is retention, which drops whole records older than <see cref="ReliabilityCriteria.HistoryRetentionDays"/> —
/// the same way <see cref="ServiceLog"/> deletes whole expired day files. Compaction runs at most once a UTC day,
/// is serialized against every append, and replaces the file atomically so a reader never sees a partial one.
/// </para>
/// <para>
/// <b>The property names are a contract.</b> <c>tools/measure-reliability-window.ps1</c> reads this file by name;
/// see <see cref="RunHistoryEntry"/>.
/// </para>
/// <para>
/// Every write is best-effort at the call site: losing a history line must never fail the refresh or the backup
/// that produced it. This type throws, and its callers log and continue.
/// </para>
/// </remarks>
public sealed class RunHistoryStore
{
    /// <summary>The file name the measurement tool looks for.</summary>
    public const string FileName = "run-history.jsonl";

    /// <summary>The <c>record</c> value for a refresh entry.</summary>
    public const string RefreshRecordKind = "refresh";

    /// <summary>The <c>record</c> value for a backup entry.</summary>
    public const string BackupRecordKind = "backup";

    /// <summary>The <c>outcome</c> value the criteria count as good.</summary>
    public const string SucceededOutcome = "Succeeded";

    /// <summary>The <c>outcome</c> value for a run that was deliberately not attempted.</summary>
    public const string SkippedOutcome = "Skipped";

    /// <summary>The <c>outcome</c> value for a run that was attempted and did not succeed.</summary>
    public const string FailedOutcome = "Failed";

    /// <summary>The <c>outcome</c> value for a run that has not reached a terminal state.</summary>
    public const string PendingOutcome = "Pending";

    /// <summary>The <c>trigger</c> value for a run the schedule started.</summary>
    public const string ScheduledTrigger = "scheduled";

    /// <summary>The <c>trigger</c> value for a run a caller started.</summary>
    public const string OnDemandTrigger = "onDemand";

    /// <summary>The <c>reason</c> value for a skip caused by the source being unreachable.</summary>
    public const string SourceUnreachableReason = "sourceUnreachable";

    private static readonly JsonSerializerOptions s_options = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _filePath;
    private readonly ILogger<RunHistoryStore> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private DateOnly _lastCompactionDate = DateOnly.MinValue;

    /// <summary>Creates the store.</summary>
    /// <param name="appDataRoot">The service's own app-data root, beside the run-record and artifact stores.</param>
    /// <param name="logger">Logger; history detail is sanitized before it is written, so no credential reaches it.</param>
    /// <param name="timeProvider">Time source for retention; injected so expiry is testable.</param>
    public RunHistoryStore(string appDataRoot, ILogger<RunHistoryStore> logger, TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appDataRoot);
        ArgumentNullException.ThrowIfNull(logger);

        _filePath = Path.Combine(appDataRoot, FileName);
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>The full path of the history file.</summary>
    public string FilePath => _filePath;

    /// <summary>How many days of history are kept.</summary>
    public static int RetentionDays => ReliabilityCriteria.HistoryRetentionDays;

    /// <summary>Records one completed refresh.</summary>
    /// <param name="record">The run record the engine produced.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task AppendRefreshAsync(RefreshRunRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        return AppendAsync(FromRefresh(record), cancellationToken);
    }

    /// <summary>Records one completed backup.</summary>
    /// <param name="record">The run record the backup engine produced.</param>
    /// <param name="artifact">The artifact produced, when one was.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task AppendBackupAsync(
        BackupRunRecord record,
        BackupArtifact? artifact,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        return AppendAsync(FromBackup(record, artifact), cancellationToken);
    }

    /// <summary>Appends one entry, then applies retention if it is due.</summary>
    /// <param name="entry">The entry to append.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task AppendAsync(RunHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var line = JsonSerializer.Serialize(entry, s_options);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // FileShare.Read so a reader — the measurement tool, or an operator with the file open — never
            // blocks a write, and a write never blocks a read.
            await using (var stream = new FileStream(
                _filePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true))
            await using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
            }

            await CompactAsync(nowUtc, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Reads every retained entry, oldest first.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// A line that cannot be parsed is skipped rather than throwing: one corrupt line is not a reason to report
    /// a service with no history at all. The measurement tool takes the same position.
    /// </remarks>
    public async Task<IReadOnlyList<RunHistoryEntry>> ReadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        var entries = new List<RunHistoryEntry>();

        foreach (var line in await File.ReadAllLinesAsync(_filePath, cancellationToken).ConfigureAwait(false))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var entry = JsonSerializer.Deserialize<RunHistoryEntry>(line, s_options);
                if (entry is not null)
                {
                    entries.Add(entry);
                }
            }
            catch (JsonException)
            {
                // Deliberately ignored; see the remarks.
            }
        }

        return entries;
    }

    /// <summary>Builds the history entry for a refresh run.</summary>
    /// <param name="record">The run record.</param>
    public static RunHistoryEntry FromRefresh(RefreshRunRecord record) => new()
    {
        Record = RefreshRecordKind,
        CycleUtc = AsUtc(record.CycleUtc),
        Shape = record.ShapeKey,
        Outcome = ToOutcomeWord(record.Outcome),
        Reason = ToReasonWord(record.Outcome),
        Detail = RefreshRunRecordStore.Sanitize(record.ErrorMessage),
        Rows = record.Outcome == RefreshRunOutcome.Succeeded ? record.RowCount ?? 0 : null,
        Trigger = record.Trigger == RefreshRunTrigger.Scheduled ? ScheduledTrigger : OnDemandTrigger
    };

    /// <summary>Builds the history entry for a backup run.</summary>
    /// <param name="record">The run record.</param>
    /// <param name="artifact">The artifact produced, when one was.</param>
    public static RunHistoryEntry FromBackup(BackupRunRecord record, BackupArtifact? artifact) => new()
    {
        Record = BackupRecordKind,
        WindowUtc = AsUtc(record.StartedUtc),
        Store = record.Store.ToDatabaseName(),
        Outcome = record.Outcome switch
        {
            BackupRunOutcome.Succeeded => SucceededOutcome,
            BackupRunOutcome.Never => PendingOutcome,
            _ => FailedOutcome
        },
        Reason = record.Outcome switch
        {
            BackupRunOutcome.ToolUnavailable => "mysqldumpUnavailable",
            BackupRunOutcome.Failed => "mysqldumpFailed",
            BackupRunOutcome.Never => "neverRun",
            _ => null
        },
        Detail = RefreshRunRecordStore.Sanitize(record.ErrorMessage),

        // The record's own path is the fallback: a successful run always carries both, but a history line that
        // claimed a success with no artifact would be worse than one that admits it has no path.
        ArtifactPath = artifact?.FilePath ?? record.ArtifactPath,
        ArtifactBytes = artifact?.SizeBytes
    };

    /// <summary>Maps a refresh outcome onto the word the criteria are written in.</summary>
    /// <param name="outcome">The run outcome.</param>
    public static string ToOutcomeWord(RefreshRunOutcome outcome) => outcome switch
    {
        RefreshRunOutcome.Succeeded => SucceededOutcome,
        RefreshRunOutcome.SkippedSourceUnreachable => SkippedOutcome,
        RefreshRunOutcome.Pending or RefreshRunOutcome.Running => PendingOutcome,
        _ => FailedOutcome
    };

    /// <summary>Maps a refresh outcome onto its short reason token, or nothing when it succeeded.</summary>
    /// <param name="outcome">The run outcome.</param>
    public static string? ToReasonWord(RefreshRunOutcome outcome) => outcome switch
    {
        RefreshRunOutcome.SkippedSourceUnreachable => SourceUnreachableReason,
        RefreshRunOutcome.FailedSchemaMismatch => "schemaMismatch",
        RefreshRunOutcome.FailedLoad => "loadFailed",
        RefreshRunOutcome.FailedUnknown => "unexpectedError",
        _ => null
    };

    /// <summary>
    /// Returns the instant an entry belongs to, for retention. Refresh entries are dated by their cycle and
    /// backup entries by their window; nothing else is a candidate.
    /// </summary>
    private static DateTime EntryUtc(RunHistoryEntry entry) =>
        entry.CycleUtc != default ? entry.CycleUtc : entry.WindowUtc;

    /// <summary>
    /// Normalizes an instant to UTC so the written JSON always ends in <c>Z</c>.
    /// </summary>
    /// <remarks>
    /// This is not cosmetic. A line written without the designator is read back as <i>local</i> time by the
    /// measurement tool's <c>[datetime]</c> cast, which shifts every entry by the host's offset and can move a
    /// record out of the window it belongs to.
    /// </remarks>
    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    /// <summary>Drops records older than the retention window, at most once a UTC day.</summary>
    /// <param name="nowUtc">The current instant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task CompactAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(nowUtc);
        if (_lastCompactionDate == today)
        {
            return;
        }

        _lastCompactionDate = today;

        if (!File.Exists(_filePath))
        {
            return;
        }

        var cutoff = nowUtc.AddDays(-RetentionDays);
        var kept = new List<string>();
        var dropped = 0;

        foreach (var line in await File.ReadAllLinesAsync(_filePath, cancellationToken).ConfigureAwait(false))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (TryReadInstant(line, out var instant) && instant < cutoff)
            {
                dropped++;
                continue;
            }

            // Anything that cannot be dated is kept. An unreadable line is evidence of a fault, and deleting
            // evidence to save bytes is the wrong trade.
            kept.Add(line);
        }

        if (dropped == 0)
        {
            return;
        }

        var temporaryPath = _filePath + ".tmp";
        await File.WriteAllLinesAsync(temporaryPath, kept, cancellationToken).ConfigureAwait(false);
        File.Move(temporaryPath, _filePath, overwrite: true);

        _logger.LogInformation(
            "Run history retention removed {Dropped} record(s) older than {Cutoff:O}; {Kept} kept.",
            dropped,
            cutoff,
            kept.Count);
    }

    /// <summary>Reads the instant a history line belongs to, when it has one.</summary>
    private static bool TryReadInstant(string line, out DateTime instant)
    {
        instant = default;

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            foreach (var name in new[] { "cycleUtc", "windowUtc" })
            {
                if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    instant = value.GetDateTime();
                    return true;
                }
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }
}
