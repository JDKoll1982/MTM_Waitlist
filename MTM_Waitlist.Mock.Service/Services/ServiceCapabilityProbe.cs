using Microsoft.Extensions.Logging;

using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// Measures what this host can currently do: Infor Visual for refresh, and each store for its backup.
/// </summary>
/// <remarks>
/// <para>
/// <b>Installable anywhere, honest about it.</b> The service runs on any machine; the work that machine
/// cannot do is disabled and reported instead of attempted and failed. That is what lets a workstation serve
/// the mirror without a Visual connection, and lets one host back up the stores it can reach while another
/// backs up the ones it can.
/// </para>
/// <para>
/// <b>The verdict is cached, and re-measured.</b> A cached answer keeps a status request, a scheduled cycle
/// and an on-demand call from opening a connection each, while the cache lifetime is short enough that a host
/// that regains access starts working again without a restart — the direction that matters, because losing
/// access is already handled by every operation reporting its own failure.
/// </para>
/// <para>
/// <b>Only one probe runs at a time,</b> so a status request and a scheduled loop that arrive together share
/// one measurement instead of racing.
/// </para>
/// </remarks>
public sealed class ServiceCapabilityProbe : IServiceCapabilityGate
{
    /// <summary>How long a measured verdict is reused before the next caller re-measures it.</summary>
    public static readonly TimeSpan DefaultCacheLifetime = TimeSpan.FromMinutes(1);

    /// <summary>Cap on the whole measurement, so an unreachable host cannot stall a loop or a status request.</summary>
    public static readonly TimeSpan DefaultProbeTimeout = TimeSpan.FromSeconds(20);

    private readonly IVisualConnectionStringProvider _visualConnectionStrings;
    private readonly IVisualConnectivityProbe _visualProbe;
    private readonly IMySqlStoreConnectivityProbe _storeProbe;
    private readonly ILogger<ServiceCapabilityProbe> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _cacheLifetime;
    private readonly TimeSpan _probeTimeout;
    private readonly SemaphoreSlim _probeGate = new(1, 1);

    private ServiceCapabilitySnapshot _current = ServiceCapabilitySnapshot.Unknown;

    /// <summary>Creates the probe.</summary>
    /// <param name="visualConnectionStrings">Reports whether Infor Visual is configured at all.</param>
    /// <param name="visualProbe">Checks whether Infor Visual accepts a connection.</param>
    /// <param name="storeProbe">Checks each store's database.</param>
    /// <param name="logger">Logger; credential material is never passed to it.</param>
    /// <param name="timeProvider">Time source, so the cache lifetime is testable.</param>
    /// <param name="cacheLifetime">How long a verdict is reused; tests lower it.</param>
    /// <param name="probeTimeout">Cap on the whole measurement; tests lower it.</param>
    public ServiceCapabilityProbe(
        IVisualConnectionStringProvider visualConnectionStrings,
        IVisualConnectivityProbe visualProbe,
        IMySqlStoreConnectivityProbe storeProbe,
        ILogger<ServiceCapabilityProbe> logger,
        TimeProvider? timeProvider = null,
        TimeSpan? cacheLifetime = null,
        TimeSpan? probeTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(visualConnectionStrings);
        ArgumentNullException.ThrowIfNull(visualProbe);
        ArgumentNullException.ThrowIfNull(storeProbe);
        ArgumentNullException.ThrowIfNull(logger);

        _visualConnectionStrings = visualConnectionStrings;
        _visualProbe = visualProbe;
        _storeProbe = storeProbe;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _cacheLifetime = cacheLifetime ?? DefaultCacheLifetime;
        _probeTimeout = probeTimeout ?? DefaultProbeTimeout;

        if (_cacheLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(cacheLifetime), _cacheLifetime, "The cache lifetime must be greater than zero.");
        }

        if (_probeTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(probeTimeout), _probeTimeout, "The probe timeout must be greater than zero.");
        }
    }

    /// <inheritdoc />
    public ServiceCapabilitySnapshot Current => Volatile.Read(ref _current);

    /// <inheritdoc />
    public async Task<ServiceCapabilitySnapshot> GetCapabilitiesAsync(
        bool reprobe = false,
        CancellationToken cancellationToken = default)
    {
        if (!reprobe && IsFresh(Current))
        {
            return Current;
        }

        await _probeGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // Re-checked under the gate: another caller may have measured while this one waited, and its
            // answer is as good as the one this caller was about to take.
            if (!reprobe && IsFresh(Current))
            {
                return Current;
            }

            var measured = await MeasureAsync(cancellationToken).ConfigureAwait(false);
            var previous = Interlocked.Exchange(ref _current, measured);

            ReportTransitions(previous, measured);

            return measured;
        }
        finally
        {
            _probeGate.Release();
        }
    }

    private bool IsFresh(ServiceCapabilitySnapshot snapshot) =>
        _timeProvider.GetUtcNow() - snapshot.ProbedUtc < _cacheLifetime;

    private async Task<ServiceCapabilitySnapshot> MeasureAsync(CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_probeTimeout);

        var visual = await MeasureVisualAsync(timeoutSource.Token, cancellationToken).ConfigureAwait(false);

        var stores = new Dictionary<BackupStore, StoreCapabilityState>();

        foreach (var store in BackupStoreExtensions.All)
        {
            stores[store] = await MeasureStoreAsync(store, timeoutSource.Token, cancellationToken).ConfigureAwait(false);
        }

        return new ServiceCapabilitySnapshot(visual, stores, _timeProvider.GetUtcNow());
    }

    private async Task<VisualCapabilityState> MeasureVisualAsync(
        CancellationToken probeToken,
        CancellationToken cancellationToken)
    {
        // Nothing configured is "unknown", not "disabled": a host that has not been pointed at Visual yet must
        // not look like a host that has been proved unable to reach it.
        string? visualConnectionString;
        try
        {
            visualConnectionString = _visualConnectionStrings.Resolve();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "The Infor Visual connection string could not be resolved; refresh is left enabled.");

            return VisualCapabilityState.Unspecified;
        }

        if (string.IsNullOrWhiteSpace(visualConnectionString))
        {
            return VisualCapabilityState.Unspecified;
        }

        try
        {
            return await _visualProbe.ProbeAsync(probeToken).ConfigureAwait(false)
                ? VisualCapabilityState.Available
                : VisualCapabilityState.Unreachable(
                    "Infor Visual did not accept a connection from this machine, so refresh is disabled here.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            // The probe's own cap fired. The source is not reachable in the time allowed, which is the same
            // answer the probe's own false would have given.
            _logger.LogInformation("The Infor Visual reachability probe timed out; refresh is disabled until it answers.");

            return VisualCapabilityState.Unreachable(
                "Infor Visual did not answer within the probe timeout, so refresh is disabled here.");
        }
        catch (Exception exception)
        {
            // The probe itself failed, which is NOT evidence that Visual is unreachable, so nothing is disabled.
            _logger.LogWarning(exception, "The Infor Visual reachability probe failed; refresh is left enabled, because a failed probe is not evidence that the source is unreachable.");

            return VisualCapabilityState.Unspecified;
        }
    }

    private async Task<StoreCapabilityState> MeasureStoreAsync(
        BackupStore store,
        CancellationToken probeToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _storeProbe.ProbeAsync(store, probeToken).ConfigureAwait(false);

            if (!result.IsConfigured)
            {
                return StoreCapabilityState.Unspecified(store);
            }

            return result.IsReachable
                ? StoreCapabilityState.Available(store)
                : StoreCapabilityState.Unreachable(
                    store,
                    result.Reason ?? $"MySQL did not accept a connection to '{store.ToDatabaseName()}' from this machine.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "The reachability probe for {Store} timed out; its backup and restore are disabled until it answers.",
                store.ToDatabaseName());

            return StoreCapabilityState.Unreachable(
                store,
                $"'{store.ToDatabaseName()}' did not answer within the probe timeout.");
        }
        catch (Exception exception)
        {
            // The probe itself failed, which is NOT evidence that the store is unreachable, so nothing is disabled.
            _logger.LogWarning(
                exception,
                "The reachability probe for {Store} failed; its backup and restore are left enabled, because a failed probe is not evidence that the store is unreachable.",
                store.ToDatabaseName());

            return StoreCapabilityState.Unspecified(store);
        }
    }

    /// <summary>
    /// Logs the edges only, so a permanently disabled capability is stated once instead of once per cycle.
    /// </summary>
    private void ReportTransitions(ServiceCapabilitySnapshot previous, ServiceCapabilitySnapshot measured)
    {
        if (previous.VisualSource.IsAvailable != measured.VisualSource.IsAvailable)
        {
            if (measured.VisualSource.IsAvailable)
            {
                _logger.LogInformation("Infor Visual is reachable again; scheduled refresh resumes.");
            }
            else
            {
                _logger.LogWarning(
                    "Refresh is disabled on this host: {Reason}",
                    measured.VisualSource.Reason);
            }
        }

        foreach (var store in BackupStoreExtensions.All)
        {
            if (previous.IsStoreAvailable(store) == measured.IsStoreAvailable(store))
            {
                continue;
            }

            if (measured.IsStoreAvailable(store))
            {
                _logger.LogInformation("Store {Store} is reachable again; its backup and restore resume.", store.ToDatabaseName());
            }
            else
            {
                _logger.LogWarning(
                    "Backup and restore are disabled for {Store} on this host: {Reason}",
                    store.ToDatabaseName(),
                    measured.StoreUnavailableReason(store));
            }
        }
    }
}
