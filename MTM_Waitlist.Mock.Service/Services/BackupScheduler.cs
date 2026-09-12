using Microsoft.Extensions.Logging;
using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// Fires each store's backup at its own configured local time (FR-009, SC-008).
/// </summary>
/// <remarks>
/// <para>
/// Scheduling is <b>per store and independent</b>: each store carries its own next-due time derived from
/// its own <see cref="BackupPolicy.ScheduleLocalTime"/>, so disabling or rescheduling one store changes no
/// other store's runs (FR-009, US6 acceptance 2). A disabled store's slot still advances, so re-enabling it
/// resumes on the schedule instead of firing immediately.
/// </para>
/// <para>
/// A failed or unavailable-tool run never stops the loop: the outcome is recorded, the next slot is
/// scheduled, and the other stores are unaffected. Nothing here is fatal.
/// </para>
/// <para>
/// Every store's own <c>BackupEngine</c> gate rejects an overlapping run, so a slow dump cannot be
/// started twice — a manual trigger and the schedule cannot collide.
/// </para>
/// <para>
/// A store this machine cannot reach has its backup <b>disabled</b> rather than repeatedly attempted: the
/// slot still advances, nothing is recorded as a run, and the reason is reported by the capability gate —
/// which is the honest answer for a host that simply does not hold that database.
/// </para>
/// </remarks>
public sealed class BackupScheduler
{
    private static readonly TimeSpan s_defaultMinimumWait = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan s_errorRetryDelay = TimeSpan.FromMinutes(5);

    private readonly BackupEngine _engine;
    private readonly Func<Models.ServiceConfiguration> _configurationAccessor;
    private readonly ILogger<BackupScheduler> _logger;
    private readonly TimeSpan _minimumWait;
    private readonly IServiceCapabilityGate? _capabilityGate;
    private readonly Dictionary<BackupStore, DateTime> _nextDueUtc = [];

    /// <summary>Creates the scheduler.</summary>
    /// <param name="engine">The engine that performs one store's backup.</param>
    /// <param name="configurationAccessor">Reads the live configuration, so schedule edits apply without a restart.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="minimumWait">Floor on the loop's sleep; tests lower it.</param>
    /// <param name="capabilityGate">
    /// Reports which stores this machine can reach. A store it reports as unreachable is skipped without a
    /// run record. Omitted or <see langword="null"/> means no gating.
    /// </param>
    public BackupScheduler(
        BackupEngine engine,
        Func<Models.ServiceConfiguration> configurationAccessor,
        ILogger<BackupScheduler> logger,
        TimeSpan? minimumWait = null,
        IServiceCapabilityGate? capabilityGate = null)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(configurationAccessor);
        ArgumentNullException.ThrowIfNull(logger);

        _engine = engine;
        _configurationAccessor = configurationAccessor;
        _logger = logger;
        _minimumWait = minimumWait ?? s_defaultMinimumWait;
        _capabilityGate = capabilityGate;
    }

    /// <summary>The next scheduled run time for a store, or <see langword="null"/> when it has not been scheduled yet.</summary>
    /// <param name="store">The store to report.</param>
    public DateTime? GetNextDueUtc(BackupStore store) =>
        _nextDueUtc.TryGetValue(store, out var due) ? due : null;

    /// <summary>
    /// Runs the per-store backup schedule until cancelled.
    /// </summary>
    /// <param name="timeProvider">Time source; supplies both "now" and the local time zone the schedule is anchored to.</param>
    /// <param name="cancellationToken">Stops the loop.</param>
    public async Task RunScheduledAsync(TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        _logger.LogInformation("Scheduled backup loop started.");

        PrimeNextDueTimes(timeProvider);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var utcNow = timeProvider.GetUtcNow().UtcDateTime;

                await RunDueStoresAsync(utcNow, timeProvider, cancellationToken).ConfigureAwait(false);

                var wait = GetTimeUntilNextDue(timeProvider.GetUtcNow().UtcDateTime, timeProvider);
                await Task.Delay(wait, timeProvider, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // A scheduling fault must never end the loop; retry after a bounded delay.
                _logger.LogError(exception, "Scheduled backup cycle failed unexpectedly; the loop continues.");

                try
                {
                    await Task.Delay(s_errorRetryDelay, timeProvider, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("Scheduled backup loop stopped.");
    }

    /// <summary>
    /// Runs every store whose slot has arrived, then advances that store's slot.
    /// </summary>
    /// <param name="utcNow">The time the due check is made against.</param>
    /// <param name="timeProvider">Time source supplying the local time zone.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stores that were attempted.</returns>
    public async Task<IReadOnlyList<BackupStore>> RunDueStoresAsync(
        DateTime utcNow,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        var configuration = _configurationAccessor();
        var attempted = new List<BackupStore>();
        var capabilities = await GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var store in BackupStoreExtensions.All)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_nextDueUtc.TryGetValue(store, out var dueUtc))
            {
                _nextDueUtc[store] = GetNextOccurrenceUtc(configuration, store, utcNow, timeProvider);
                continue;
            }

            if (utcNow < dueUtc)
            {
                continue;
            }

            // Advance first: a scheduling problem in the run must not re-fire the same slot forever.
            _nextDueUtc[store] = GetNextOccurrenceUtc(configuration, store, utcNow, timeProvider);

            if (!configuration.BackupPolicies[store].IsEnabled)
            {
                _logger.LogInformation(
                    "Backup slot for {Store} arrived but the store is disabled; no backup was taken.",
                    store.ToDatabaseName());
                continue;
            }

            if (capabilities is not null && !capabilities.IsStoreAvailable(store))
            {
                // Disabled on this host, not failed: nothing is recorded as a run, the slot has already
                // advanced, and the capability gate reports the reason.
                _logger.LogWarning(
                    "Backup slot for {Store} arrived but this machine cannot reach that database, so its backup and restore are disabled here: {Reason}",
                    store.ToDatabaseName(),
                    capabilities.StoreUnavailableReason(store));
                continue;
            }

            attempted.Add(store);

            try
            {
                await _engine.RunAsync(store, isSafetySnapshot: false, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                // One store's problem is that store's problem: the others still run on their own schedule.
                _logger.LogError(
                    exception,
                    "Scheduled backup for {Store} failed; other stores are unaffected.",
                    store.ToDatabaseName());
            }
        }

        return attempted;
    }

    /// <summary>
    /// Reads the host's capabilities, or <see langword="null"/> when there is nothing to gate on.
    /// </summary>
    /// <remarks>
    /// A gate that cannot answer returns <see langword="null"/>, which disables nothing: a probe failure is not
    /// evidence that a store is unreachable, and the run's own outcome already reports a real failure.
    /// </remarks>
    private async Task<ServiceCapabilitySnapshot?> GetCapabilitiesAsync(CancellationToken cancellationToken)
    {
        if (_capabilityGate is null)
        {
            return null;
        }

        try
        {
            return await _capabilityGate
                .GetCapabilitiesAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "The host capability check failed; the scheduled backup continues, because a failed probe is not evidence that a store is unreachable.");

            return null;
        }
    }

    /// <summary>
    /// Seeds a next-due time for every store, for a store missing one.
    /// </summary>
    private void PrimeNextDueTimes(TimeProvider timeProvider)
    {
        var configuration = _configurationAccessor();
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var store in BackupStoreExtensions.All)
        {
            if (!_nextDueUtc.ContainsKey(store))
            {
                _nextDueUtc[store] = GetNextOccurrenceUtc(configuration, store, utcNow, timeProvider);
                _logger.LogInformation(
                    "Store {Store} scheduled next at {NextDueUtc:u}.",
                    store.ToDatabaseName(),
                    _nextDueUtc[store]);
            }
        }
    }

    private TimeSpan GetTimeUntilNextDue(DateTime utcNow, TimeProvider timeProvider)
    {
        if (_nextDueUtc.Count == 0)
        {
            return TimeSpan.FromHours(1);
        }

        var soonest = _nextDueUtc.Values.Min();
        var wait = soonest - utcNow;

        return wait > _minimumWait ? wait : _minimumWait;
    }

    /// <summary>
    /// The next UTC time a store's local schedule time occurs, strictly after <paramref name="utcNow"/>.
    /// </summary>
    private static DateTime GetNextOccurrenceUtc(
        Models.ServiceConfiguration configuration,
        BackupStore store,
        DateTime utcNow,
        TimeProvider timeProvider)
    {
        var localNow = TimeZoneInfo.ConvertTime(
            new DateTimeOffset(utcNow, TimeSpan.Zero),
            timeProvider.LocalTimeZone);

        var schedule = configuration.BackupPolicies[store].ScheduleLocalTime;
        var candidateLocal = localNow.Date.Add(schedule.ToTimeSpan());

        if (candidateLocal <= localNow.DateTime)
        {
            candidateLocal = candidateLocal.AddDays(1);
        }

        var unspecified = DateTime.SpecifyKind(candidateLocal, DateTimeKind.Unspecified);

        try
        {
            return TimeZoneInfo.ConvertTimeToUtc(unspecified, timeProvider.LocalTimeZone);
        }
        catch (ArgumentException)
        {
            // The slot does not exist on the local clock (DST spring-forward); use the next real time.
            return TimeZoneInfo.ConvertTimeToUtc(unspecified.AddHours(1), timeProvider.LocalTimeZone);
        }
    }
}
