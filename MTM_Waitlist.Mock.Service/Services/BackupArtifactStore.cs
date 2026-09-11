using System.Text.Json;
using System.Text.Json.Serialization;
using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// Durable, service-local record of every backup artifact and the last run per store (FR-009/FR-013).
/// </summary>
/// <remarks>
/// <para>
/// The artifact list is what makes per-store independence verifiable: each store's artifacts are
/// recorded, pruned, and reported on their own, so disabling or rescheduling one store cannot change
/// another's (FR-009, US6 acceptance 2).
/// </para>
/// <para>
/// Persistence is deliberately not in MySQL (data-model.md §4): a restore replaces a whole store, so
/// the record of what was backed up must survive the restore. Saves are all-or-nothing.
/// </para>
/// </remarks>
public sealed class BackupArtifactStore
{
    /// <summary>File name of the persisted artifact records under the service app-data root.</summary>
    public const string FileName = "backup-artifacts.json";

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<BackupArtifact> _artifacts = [];
    private readonly Dictionary<BackupStore, BackupRunRecord> _lastRuns = [];

    /// <summary>Creates a store rooted at the given service app-data folder.</summary>
    /// <param name="appDataRoot">Absolute path to the service's own app-data folder.</param>
    public BackupArtifactStore(string appDataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appDataRoot);
        _filePath = Path.Combine(appDataRoot, FileName);
    }

    /// <summary>Absolute path of the persisted record file.</summary>
    public string FilePath => _filePath;

    /// <summary>
    /// Loads the persisted records. A missing or corrupt file yields an empty set rather than a failure.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            _artifacts.Clear();
            _lastRuns.Clear();

            if (!File.Exists(_filePath))
            {
                return;
            }

            await using var stream = File.OpenRead(_filePath);

            PersistedState? state;
            try
            {
                state = await JsonSerializer
                    .DeserializeAsync<PersistedState>(stream, s_jsonOptions, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (JsonException)
            {
                // A hand-edited or truncated file must not stop the service from recording new runs.
                return;
            }

            foreach (var artifact in state?.Artifacts ?? [])
            {
                if (!string.IsNullOrWhiteSpace(artifact.FilePath))
                {
                    _artifacts.Add(artifact);
                }
            }

            foreach (var pair in state?.LastRuns ?? [])
            {
                _lastRuns[pair.Key] = pair.Value;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Returns one store's artifacts, newest first, including pruned ones (which are reported as such).
    /// </summary>
    /// <param name="store">The store to list.</param>
    public IReadOnlyList<BackupArtifact> GetArtifacts(BackupStore store) =>
        _artifacts
            .Where(artifact => artifact.Store == store)
            .OrderByDescending(artifact => artifact.CreatedUtc)
            .ToList();

    /// <summary>Returns one store's last run, or <see langword="null"/> when it has never run.</summary>
    /// <param name="store">The store to report.</param>
    public BackupRunRecord? GetLastRun(BackupStore store) =>
        _lastRuns.TryGetValue(store, out var record) ? record : null;

    /// <summary>The number of artifacts a store currently retains.</summary>
    /// <param name="store">The store to count.</param>
    public int GetRetainedCount(BackupStore store) =>
        _artifacts.Count(artifact => artifact.Store == store && artifact.IsRetained);

    /// <summary>
    /// Records one run and, when the run produced an artifact, that artifact.
    /// </summary>
    /// <param name="record">The run outcome to record.</param>
    /// <param name="artifact">The artifact produced, or <see langword="null"/> when none was.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RecordAsync(
        BackupRunRecord record,
        BackupArtifact? artifact = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (artifact is not null)
            {
                _artifacts.Add(artifact);
            }

            _lastRuns[record.Store] = record;
            await SaveCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Enforces a store's retention count, pruning the oldest artifacts beyond the limit (FR-009).
    /// </summary>
    /// <param name="store">The store whose retention is being enforced.</param>
    /// <param name="retentionCount">How many artifacts to keep.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The artifacts that were pruned by this call.</returns>
    /// <remarks>
    /// <para>
    /// Two rules are absolute. A <b>safety snapshot is never pruned</b> — it is the recovery path for a
    /// failed restore, and it is also not counted against the limit, so retention can never push it off
    /// the end of the list. And a pruned artifact is <b>marked</b> rather than forgotten, so a later
    /// restore picker can explain that the file is no longer available instead of showing it as an
    /// unexplained gap.
    /// </para>
    /// <para>
    /// A file that has already been deleted outside the service is still marked and counted; pruning is
    /// idempotent and never fails because a file is missing.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<BackupArtifact>> PruneAsync(
        BackupStore store,
        int retentionCount,
        CancellationToken cancellationToken = default)
    {
        if (retentionCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(retentionCount), retentionCount, "Retention must be at least 1.");
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var prunable = _artifacts
                .Where(artifact => artifact.Store == store && artifact.IsRetained && !artifact.IsSafetySnapshot)
                .OrderByDescending(artifact => artifact.CreatedUtc)
                .ToList();

            if (prunable.Count <= retentionCount)
            {
                return [];
            }

            var pruned = new List<BackupArtifact>();

            foreach (var artifact in prunable.Skip(retentionCount))
            {
                var index = _artifacts.IndexOf(artifact);
                var marked = artifact with { IsRetained = false };
                _artifacts[index] = marked;
                pruned.Add(marked);

                TryDeleteFile(artifact.FilePath);
            }

            if (pruned.Count > 0)
            {
                await SaveCoreAsync(cancellationToken).ConfigureAwait(false);
            }

            return pruned;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // A locked file stays on disk; the record still marks it pruned so retention is honoured
            // logically and the next run can remove it.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private async Task SaveCoreAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        var state = new PersistedState
        {
            Artifacts = [.. _artifacts],
            LastRuns = new Dictionary<BackupStore, BackupRunRecord>(_lastRuns)
        };

        var temporaryPath = _filePath + ".tmp";

        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, state, s_jsonOptions, cancellationToken).ConfigureAwait(false);
        }

        File.Move(temporaryPath, _filePath, overwrite: true);
    }

    /// <summary>Serialization shape; kept private so the operator-facing types stay authoritative.</summary>
    private sealed record PersistedState
    {
        public List<BackupArtifact> Artifacts { get; init; } = [];

        public Dictionary<BackupStore, BackupRunRecord> LastRuns { get; init; } = [];
    }
}
