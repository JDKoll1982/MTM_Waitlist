using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Service.Api;
using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// The behavior behind every API endpoint, separated from the HTTP wiring so it is unit-testable.
/// </summary>
/// <remarks>
/// <para>
/// Nothing here touches <see cref="RestoreService"/>. Restore is host-only and must remain unreachable
/// from the network surface even for an approved operator role, so the dependency simply does not exist
/// (FR-010/FR-023, <c>contracts/mock-service-http-api.md</c> §5).
/// </para>
/// <para>
/// Read-only operations — status and artifact listing — have no side effects: they never trigger a
/// refresh or a backup.
/// </para>
/// </remarks>
public sealed class ServiceApiOperations
{
    /// <summary>Reported service version, so an operator can tell which build answered.</summary>
    public const string ServiceVersion = "1.0.0";

    private readonly RefreshEngine _refreshEngine;
    private readonly RefreshShapeCatalogProvider _catalogProvider;
    private readonly RefreshRunRecordStore _refreshRunRecordStore;
    private readonly BackupEngine _backupEngine;
    private readonly BackupArtifactStore _backupArtifactStore;
    private readonly IVisualShapeFreshnessReader _freshnessReader;
    private readonly IVisualConnectivityProbe _connectivityProbe;
    private readonly ServiceConfigurationStore _configurationStore;
    private readonly AutoStartReconciliation? _autoStartState;
    private readonly TimeProvider _timeProvider;
    private readonly DateTimeOffset _startedUtc;

    /// <summary>Creates the operations facade.</summary>
    /// <param name="refreshEngine">Runs refresh cycles.</param>
    /// <param name="catalogProvider">Supplies the validated shape catalog and its exclusion reasons.</param>
    /// <param name="refreshRunRecordStore">Supplies per-shape last-run status.</param>
    /// <param name="backupEngine">Runs on-demand backups.</param>
    /// <param name="backupArtifactStore">Supplies per-store artifact and last-run status.</param>
    /// <param name="freshnessReader">Supplies cached-data age and seed-only state.</param>
    /// <param name="connectivityProbe">Reports whether Infor Visual is reachable right now.</param>
    /// <param name="configurationStore">Supplies the persisted configuration.</param>
    /// <param name="autoStartState">The startup auto-start reconciliation result, when one has run.</param>
    /// <param name="timeProvider">Time source.</param>
    public ServiceApiOperations(
        RefreshEngine refreshEngine,
        RefreshShapeCatalogProvider catalogProvider,
        RefreshRunRecordStore refreshRunRecordStore,
        BackupEngine backupEngine,
        BackupArtifactStore backupArtifactStore,
        IVisualShapeFreshnessReader freshnessReader,
        IVisualConnectivityProbe connectivityProbe,
        ServiceConfigurationStore configurationStore,
        AutoStartReconciliation? autoStartState = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(refreshEngine);
        ArgumentNullException.ThrowIfNull(catalogProvider);
        ArgumentNullException.ThrowIfNull(refreshRunRecordStore);
        ArgumentNullException.ThrowIfNull(backupEngine);
        ArgumentNullException.ThrowIfNull(backupArtifactStore);
        ArgumentNullException.ThrowIfNull(freshnessReader);
        ArgumentNullException.ThrowIfNull(connectivityProbe);
        ArgumentNullException.ThrowIfNull(configurationStore);

        _refreshEngine = refreshEngine;
        _catalogProvider = catalogProvider;
        _refreshRunRecordStore = refreshRunRecordStore;
        _backupEngine = backupEngine;
        _backupArtifactStore = backupArtifactStore;
        _freshnessReader = freshnessReader;
        _connectivityProbe = connectivityProbe;
        _configurationStore = configurationStore;
        _autoStartState = autoStartState;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _startedUtc = _timeProvider.GetUtcNow();
    }

    /// <summary>
    /// Builds the status payload: per-shape and per-store last-run state, freshness, and tooling.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The status payload, or an error result when the cache cannot be read.</returns>
    public async Task<ServiceApiOutcome<ServiceApiContracts.ServiceStatusPayload>> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var configuration = _configurationStore.Current;

        // A probe failure means "not reachable right now" and is reported, not thrown: status must work
        // while the source is down — that is precisely when an operator asks for it.
        var visualReachable = false;
        try
        {
            visualReachable = await _connectivityProbe.ProbeAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            visualReachable = false;
        }

        IReadOnlyList<VisualShapeFreshness> freshness = [];

        try
        {
            freshness = await _freshnessReader.GetFreshnessAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // A cache that cannot be read leaves the freshness columns empty; the run records and tooling
            // state in the rest of the payload are still what the operator needs to act on.
            freshness = [];
        }

        var freshnessByKey = freshness.ToDictionary(entry => entry.ShapeKey, StringComparer.Ordinal);

        var shapes = new List<ServiceApiContracts.ShapeStatusPayload>();
        foreach (var entry in _catalogProvider.Entries)
        {
            var lastRun = _refreshRunRecordStore.GetLastRun(entry.Shape.Key);
            freshnessByKey.TryGetValue(entry.Shape.Key, out var shapeFreshness);

            shapes.Add(new ServiceApiContracts.ShapeStatusPayload(
                entry.Shape.Key,
                entry.Shape.IsEnabled,
                lastRun?.FinishedUtc ?? lastRun?.StartedUtc,
                lastRun is null ? null : ToOutcomeText(lastRun.Outcome),
                lastRun?.RowCount,
                shapeFreshness?.RefreshedUtc,
                shapeFreshness?.IsSeedContentOnly ?? true,
                entry.InvalidReason));
        }

        // A shape with artifacts but no catalog entry is never refreshed. It is reported as its own row
        // rather than omitted, so the gap is visible instead of silent (FR-016/FR-020).
        foreach (var unregisteredKey in _catalogProvider.UnregisteredShapeKeys)
        {
            shapes.Add(new ServiceApiContracts.ShapeStatusPayload(
                unregisteredKey,
                IsEnabled: false,
                LastRunUtc: null,
                LastOutcome: null,
                LastRowCount: null,
                RefreshedUtc: null,
                IsSeedContentOnly: true,
                ValidationError:
                    "Present in mtm_mock but not registered in the shape catalog, so nothing refreshes it."));
        }

        var toolAvailable = await _backupEngine.IsToolAvailableAsync(cancellationToken).ConfigureAwait(false);

        var backups = new List<ServiceApiContracts.BackupStatusPayload>();
        foreach (var store in BackupStoreExtensions.All)
        {
            var policy = configuration.BackupPolicies[store];
            var lastRun = _backupArtifactStore.GetLastRun(store);

            backups.Add(new ServiceApiContracts.BackupStatusPayload(
                store.ToDatabaseName(),
                policy.IsEnabled,
                lastRun?.FinishedUtc ?? lastRun?.StartedUtc,
                lastRun is null ? null : ToOutcomeText(lastRun.Outcome),
                lastRun?.ArtifactPath,
                _backupArtifactStore.GetRetainedCount(store),
                toolAvailable));
        }

        var payload = new ServiceApiContracts.ServiceStatusPayload(
            ServiceVersion,
            _startedUtc,
            visualReachable,
            ServiceOperatorRoles.DisplayText,
            (int)Math.Round(configuration.RefreshInterval.TotalMinutes),
            shapes,
            backups,
            _autoStartState is null
                ? null
                : new ServiceApiContracts.AutoStartStatusPayload(
                    _autoStartState.SettingEnabled,
                    _autoStartState.WasRegisteredAtLogon,
                    _autoStartState.IsReconciled,
                    _autoStartState.Message));

        return ServiceApiOutcome<ServiceApiContracts.ServiceStatusPayload>.Ok(payload);
    }

    /// <summary>
    /// Runs an on-demand refresh, optionally limited to named shapes.
    /// </summary>
    /// <param name="shapeKeys">
    /// Shape keys to refresh, or <see langword="null"/>/empty for every enabled shape.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// `200` with per-shape outcomes, `400` when a key is unknown, or `409` when a cycle is already running.
    /// </returns>
    public async Task<ServiceApiOutcome<ServiceApiContracts.RefreshResponsePayload>> RunRefreshAsync(
        IReadOnlyCollection<string>? shapeKeys,
        CancellationToken cancellationToken = default)
    {
        var refreshable = _catalogProvider.RefreshableShapes;
        var selected = new List<VisualReadShape>();

        if (shapeKeys is null || shapeKeys.Count == 0)
        {
            selected.AddRange(refreshable);
        }
        else
        {
            foreach (var key in shapeKeys)
            {
                var match = refreshable.FirstOrDefault(shape => string.Equals(shape.Key, key, StringComparison.Ordinal));

                if (match is null)
                {
                    return ServiceApiOutcome<ServiceApiContracts.RefreshResponsePayload>.Fail(
                        400,
                        "unknownShape",
                        $"'{key}' is not a known, enabled shape.");
                }

                selected.Add(match);
            }
        }

        var startedUtc = _timeProvider.GetUtcNow();
        var runId = Guid.NewGuid().ToString("N");

        // The gated seam, not a per-shape loop over the ungated RefreshShapeAsync: acquiring the cycle gate
        // is what keeps an on-demand refresh from overlapping a scheduled cycle (FR-008, and the contract's
        // single-cycle rule). A null result means the gate was already held, which is the 409.
        var records = await _refreshEngine
            .TryRunShapesAsync(selected, cancellationToken)
            .ConfigureAwait(false);

        if (records is null)
        {
            return ServiceApiOutcome<ServiceApiContracts.RefreshResponsePayload>.Fail(
                409,
                "refreshInProgress",
                "A refresh cycle is already running.");
        }

        var results = new List<ServiceApiContracts.RefreshShapeOutcomePayload>(records.Count);

        foreach (var record in records)
        {
            results.Add(new ServiceApiContracts.RefreshShapeOutcomePayload(
                record.ShapeKey,
                ToOutcomeText(record.Outcome),
                record.RowCount,
                record.DurationMs,
                record.ErrorMessage));
        }

        return ServiceApiOutcome<ServiceApiContracts.RefreshResponsePayload>.Ok(
            new ServiceApiContracts.RefreshResponsePayload(
                runId,
                startedUtc,
                _timeProvider.GetUtcNow(),
                results));
    }

    /// <summary>
    /// Runs one store's backup now.
    /// </summary>
    /// <param name="storeName">The store's database name, as reported by the status payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>`200` with the outcome (including <c>toolUnavailable</c>), `400` for an unknown store, `409` while one runs.</returns>
    public async Task<ServiceApiOutcome<ServiceApiContracts.BackupResponsePayload>> RunBackupAsync(
        string? storeName,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolveStore(storeName, out var store))
        {
            return ServiceApiOutcome<ServiceApiContracts.BackupResponsePayload>.Fail(
                400,
                "unknownStore",
                $"'{storeName}' is not one of the four stores.");
        }

        if (_backupEngine.IsRunning(store))
        {
            return ServiceApiOutcome<ServiceApiContracts.BackupResponsePayload>.Fail(
                409,
                "refreshInProgress",
                $"A backup for '{store.ToDatabaseName()}' is already running.");
        }

        var record = await _backupEngine.RunAsync(store, isSafetySnapshot: false, cancellationToken)
            .ConfigureAwait(false);

        var artifact = _backupArtifactStore
            .GetArtifacts(store)
            .FirstOrDefault(candidate => candidate.FilePath == record.ArtifactPath);

        // toolUnavailable is a 200 with no artifact: an unavailable tool is a reported condition, not a
        // request error, and never a partial or zero-length file presented as a backup (FR-013).
        return ServiceApiOutcome<ServiceApiContracts.BackupResponsePayload>.Ok(
            new ServiceApiContracts.BackupResponsePayload(
                store.ToDatabaseName(),
                ToOutcomeText(record.Outcome),
                artifact is null ? null : ToArtifactPayload(artifact)));
    }

    /// <summary>
    /// Lists backup artifacts, newest first, optionally limited to one store.
    /// </summary>
    /// <param name="storeName">The store's database name, or <see langword="null"/> for every store.</param>
    /// <returns>`200` with the artifacts, or `400` for an unknown store.</returns>
    public ServiceApiOutcome<ServiceApiContracts.BackupListPayload> ListBackups(string? storeName)
    {
        IReadOnlyList<BackupStore> stores;

        if (string.IsNullOrWhiteSpace(storeName))
        {
            stores = BackupStoreExtensions.All;
        }
        else if (TryResolveStore(storeName, out var store))
        {
            stores = [store];
        }
        else
        {
            return ServiceApiOutcome<ServiceApiContracts.BackupListPayload>.Fail(
                400,
                "unknownStore",
                $"'{storeName}' is not one of the four stores.");
        }

        var artifacts = stores
            .SelectMany(store => _backupArtifactStore.GetArtifacts(store))
            .OrderByDescending(artifact => artifact.CreatedUtc)
            .Select(ToArtifactPayload)
            .ToList();

        return ServiceApiOutcome<ServiceApiContracts.BackupListPayload>.Ok(
            new ServiceApiContracts.BackupListPayload(artifacts));
    }

    /// <summary>Maps a store's database name back onto the fixed store set.</summary>
    private static bool TryResolveStore(string? storeName, out BackupStore store)
    {
        foreach (var candidate in BackupStoreExtensions.All)
        {
            if (string.Equals(candidate.ToDatabaseName(), storeName, StringComparison.OrdinalIgnoreCase))
            {
                store = candidate;
                return true;
            }
        }

        store = default;
        return false;
    }

    private static ServiceApiContracts.ArtifactPayload ToArtifactPayload(BackupArtifact artifact) =>
        new(
            artifact.ArtifactId,
            artifact.Store.ToDatabaseName(),
            artifact.CreatedUtc,
            artifact.FilePath,
            artifact.SizeBytes,
            artifact.IsRetained,
            artifact.IsSafetySnapshot);

    /// <summary>The wire text for a refresh outcome; the contract fixes these spellings.</summary>
    private static string ToOutcomeText(RefreshRunOutcome outcome) => outcome switch
    {
        RefreshRunOutcome.Succeeded => "succeeded",
        RefreshRunOutcome.SkippedSourceUnreachable => "skippedSourceUnreachable",
        RefreshRunOutcome.FailedSchemaMismatch => "failedSchemaMismatch",
        RefreshRunOutcome.FailedLoad => "failedLoad",
        RefreshRunOutcome.Running => "running",
        RefreshRunOutcome.Pending => "pending",
        _ => "failedUnknown"
    };

    /// <summary>The wire text for a backup outcome.</summary>
    private static string ToOutcomeText(BackupRunOutcome outcome) => outcome switch
    {
        BackupRunOutcome.Succeeded => "succeeded",
        BackupRunOutcome.ToolUnavailable => "toolUnavailable",
        BackupRunOutcome.Failed => "failed",
        _ => "never"
    };
}
