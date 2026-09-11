using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// Catalog-driven refresh of the <c>mtm_mock</c> mirror.
/// </summary>
/// <remarks>
/// <para>
/// The engine is driven entirely by the shape catalog: adding a sixth read shape requires no change
/// here (FR-016). For each shape it asks the payload source for the complete result, then has the
/// shape's refresh procedure load and atomically swap it in.
/// </para>
/// <para>
/// <b>Unreachable source (FR-008):</b> when Infor Visual cannot be reached, the shape is
/// <i>skipped</i> and logged as <see cref="RefreshRunOutcome.SkippedSourceUnreachable"/> — a normal
/// outcome. The refresh procedure is never called, so the live snapshot is left exactly as it was and
/// the next cycle proceeds on schedule.
/// </para>
/// <para>
/// <b>Scheduling (US3):</b> <see cref="RunScheduledAsync"/> owns the loop. The schedule is a grid
/// anchored at <b>local midnight</b> — with the shipped 3-hour default the cache refreshes at 00:00,
/// 03:00, 06:00, 09:00, 12:00, 15:00, 18:00 and 21:00 server-local time — so a slow or skipped cycle
/// cannot drag later runs off those times. A shape that declares
/// <see cref="VisualReadShape.RefreshIntervalOverride"/> gets its own grid. Exactly one cycle runs at
/// a time: a second request is rejected instead of overlapped, and a cycle that throws never stops
/// the loop.
/// </para>
/// </remarks>
public sealed class RefreshEngine
{
    private static readonly Regex s_secretPattern = new(
        @"(password|pwd|user\s*id|uid)\s*=\s*[^;]*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Floor on the scheduled loop's wait. Without a floor an already-due shape would produce a
    /// zero-length wait and spin the loop.
    /// </summary>
    private static readonly TimeSpan s_defaultMinimumScheduledWait = TimeSpan.FromSeconds(15);

    /// <summary>Wait before retrying after an unexpected loop error, so a fault cannot spin.</summary>
    private static readonly TimeSpan s_scheduledErrorRetryDelay = TimeSpan.FromMinutes(1);

    private readonly RefreshShapeCatalogProvider _catalogProvider;
    private readonly IVisualShapePayloadSource _payloadSource;
    private readonly IMockMirrorRefreshWriter _mirrorWriter;
    private readonly ILogger<RefreshEngine> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _refreshInterval;
    private readonly TimeSpan _minimumScheduledWait;
    private readonly Dictionary<string, DateTime> _nextDueUtc = new(StringComparer.Ordinal);

    private IReadOnlyList<RefreshRunRecord> _lastRunRecords = [];
    private int _cycleGate;

    /// <summary>
    /// Creates the engine.
    /// </summary>
    /// <param name="catalogProvider">Supplies the validated, enabled shapes to refresh.</param>
    /// <param name="payloadSource">Produces each shape's complete result from Infor Visual.</param>
    /// <param name="mirrorWriter">Swaps each shape's new snapshot into the live mirror.</param>
    /// <param name="logger">Logger; credential material is never passed to it.</param>
    /// <param name="refreshInterval">
    /// The service's global refresh interval (<see cref="Models.ServiceConfiguration.RefreshInterval"/>).
    /// A shape's <see cref="VisualReadShape.RefreshIntervalOverride"/> takes precedence over it.
    /// The interval is a grid anchored at local midnight, so the shipped default of 3 hours lands on
    /// the eight times operators expect rather than "3 hours from whenever the service started".
    /// </param>
    /// <param name="minimumScheduledWait">
    /// Floor on the scheduled loop's sleep. Defaults to 15 seconds; tests lower it.
    /// </param>
    /// <param name="timeProvider">
    /// Time source. Supplies both the current time and the <b>local time zone</b> the schedule grid is
    /// anchored to, which is what makes the cadence testable.
    /// </param>
    public RefreshEngine(
        RefreshShapeCatalogProvider catalogProvider,
        IVisualShapePayloadSource payloadSource,
        IMockMirrorRefreshWriter mirrorWriter,
        ILogger<RefreshEngine> logger,
        TimeSpan? refreshInterval = null,
        TimeSpan? minimumScheduledWait = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(catalogProvider);
        ArgumentNullException.ThrowIfNull(payloadSource);
        ArgumentNullException.ThrowIfNull(mirrorWriter);
        ArgumentNullException.ThrowIfNull(logger);

        _refreshInterval = refreshInterval ?? TimeSpan.FromHours(3);
        if (_refreshInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(refreshInterval), _refreshInterval, "The global refresh interval must be greater than zero.");
        }

        _minimumScheduledWait = minimumScheduledWait ?? s_defaultMinimumScheduledWait;
        if (_minimumScheduledWait <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumScheduledWait), _minimumScheduledWait, "The minimum scheduled wait must be greater than zero.");
        }

        _catalogProvider = catalogProvider;
        _payloadSource = payloadSource;
        _mirrorWriter = mirrorWriter;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Raised after each shape attempt so the run-record store and status surface can observe it.</summary>
    public event EventHandler<RefreshRunRecord>? RunCompleted;

    /// <summary>The records produced by the most recent <see cref="RefreshCycleAsync"/> call.</summary>
    public IReadOnlyList<RefreshRunRecord> LastRunRecords => _lastRunRecords;

    /// <summary>Whether a refresh cycle is currently running.</summary>
    public bool IsCycleRunning => Volatile.Read(ref _cycleGate) == 1;

    /// <summary>
    /// The interval that applies to a shape: its own override when declared, otherwise the service's
    /// global interval.
    /// </summary>
    /// <param name="shape">The catalog shape.</param>
    public TimeSpan GetInterval(VisualReadShape shape)
    {
        ArgumentNullException.ThrowIfNull(shape);
        return shape.RefreshIntervalOverride ?? _refreshInterval;
    }

    /// <summary>
    /// Runs one refresh cycle over every enabled, validated shape.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// One run record per refreshable shape, or an empty list when a cycle was already running. Use
    /// <see cref="TryRunCycleAsync"/> when the caller must distinguish "rejected because busy" from
    /// "ran and produced no records".
    /// </returns>
    public async Task<IReadOnlyList<RefreshRunRecord>> RefreshCycleAsync(CancellationToken cancellationToken = default)
    {
        var records = await TryRunShapesAsync(
            _catalogProvider.RefreshableShapes,
            _timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken).ConfigureAwait(false);

        return records ?? [];
    }

    /// <summary>
    /// Runs one cycle over every refreshable shape unless a cycle is already running.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The run records, or <see langword="null"/> when a cycle is already running.</returns>
    /// <remarks>
    /// One cycle at a time is required by FR-008 and is what the on-demand API reports as a conflict:
    /// two overlapping cycles would contend over the same stage tables and could report a partially
    /// loaded snapshot as the current one.
    /// </remarks>
    public Task<IReadOnlyList<RefreshRunRecord>?> TryRunCycleAsync(CancellationToken cancellationToken = default) =>
        TryRunShapesAsync(_catalogProvider.RefreshableShapes, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

    /// <summary>
    /// Runs the given shapes as one gated cycle, unless a cycle is already running.
    /// </summary>
    /// <param name="shapes">The shapes to refresh.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The run records, or <see langword="null"/> when a cycle is already running.</returns>
    /// <remarks>
    /// <para>
    /// This is the seam every on-demand caller must use. Reading <see cref="IsCycleRunning"/> and then
    /// calling <see cref="RefreshShapeAsync"/> is <b>not</b> equivalent: that is a check-then-act race in
    /// which the scheduled loop — or a second request — can start between the check and the call, letting
    /// two writers reach the same stage twin and the same <c>RENAME</c> swap. Acquiring the cycle gate here
    /// is what makes the single-cycle guarantee (FR-008, <c>contracts/mock-service-http-api.md</c> §2) hold
    /// on the on-demand path as well as the scheduled one.
    /// </para>
    /// <para>
    /// A caller that receives <see langword="null"/> must report the conflict (the API's <c>409</c>) rather
    /// than retrying immediately: the running cycle already covers those shapes.
    /// </para>
    /// </remarks>
    public Task<IReadOnlyList<RefreshRunRecord>?> TryRunShapesAsync(
        IReadOnlyList<VisualReadShape> shapes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shapes);

        return TryRunShapesAsync(shapes, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
    }

    /// <summary>
    /// Runs the shapes whose interval has elapsed at <paramref name="utcNow"/> — the scheduled loop's
    /// unit of work.
    /// </summary>
    /// <param name="utcNow">The current UTC time the due check is made against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The run records, or <see langword="null"/> when a cycle is already running.</returns>
    public Task<IReadOnlyList<RefreshRunRecord>?> TryRunDueShapesAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var due = _catalogProvider.RefreshableShapes.Where(shape => IsDue(shape, utcNow)).ToList();
        return due.Count == 0
            ? Task.FromResult<IReadOnlyList<RefreshRunRecord>?>([])
            : TryRunShapesAsync(due, utcNow, cancellationToken);
    }

    /// <summary>
    /// Runs the scheduled refresh loop until cancelled.
    /// </summary>
    /// <param name="timeProvider">Time source for scheduling; the parameter keeps the loop testable.</param>
    /// <param name="cancellationToken">Stops the loop.</param>
    /// <remarks>
    /// The loop never treats an unreachable source or an unexpected error as fatal: a skipped cycle is
    /// a normal outcome, an error is logged, and the next cycle is attempted on schedule (FR-008,
    /// US3 acceptance 2/3/4).
    /// </remarks>
    public async Task RunScheduledAsync(TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        _logger.LogInformation("Scheduled refresh loop started. Global interval {Interval}. Minimum wait {MinimumWait}.", _refreshInterval, _minimumScheduledWait);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await TryRunDueShapesAsync(timeProvider.GetUtcNow().UtcDateTime, cancellationToken).ConfigureAwait(false);

                var wait = GetTimeUntilNextDue(timeProvider.GetUtcNow().UtcDateTime);
                await Task.Delay(wait, timeProvider, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // A cycle must never end the loop; retry after a bounded delay instead of spinning.
                _logger.LogError(exception, "Scheduled refresh cycle failed unexpectedly; the loop continues and retries.");

                try
                {
                    await Task.Delay(s_scheduledErrorRetryDelay, timeProvider, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("Scheduled refresh loop stopped.");
    }

    private bool IsDue(VisualReadShape shape, DateTime utcNow) =>
        !_nextDueUtc.TryGetValue(shape.Key, out var dueUtc) || utcNow >= dueUtc;

    /// <summary>
    /// How long the loop may sleep: until the soonest shape is due, floored so an already-due shape
    /// cannot produce a zero-length wait.
    /// </summary>
    private TimeSpan GetTimeUntilNextDue(DateTime utcNow)
    {
        var shapes = _catalogProvider.RefreshableShapes;
        if (shapes.Count == 0)
        {
            return _refreshInterval;
        }

        var soonestDueUtc = shapes.Min(shape => _nextDueUtc.TryGetValue(shape.Key, out var dueUtc) ? dueUtc : utcNow);
        var wait = soonestDueUtc - utcNow;

        return wait > _minimumScheduledWait ? wait : _minimumScheduledWait;
    }

    /// <summary>
    /// Runs the given shapes one at a time behind the re-entrancy guard.
    /// </summary>
    /// <param name="shapes">The shapes to run.</param>
    /// <param name="utcNow">The time this cycle is considered to have run at; the next due time is derived from it.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The records, or <see langword="null"/> when a cycle was already running.</returns>
    private async Task<IReadOnlyList<RefreshRunRecord>?> TryRunShapesAsync(
        IReadOnlyList<VisualReadShape> shapes,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _cycleGate, 1, 0) != 0)
        {
            _logger.LogInformation("A refresh cycle is already running; this request was rejected rather than overlapped.");
            return null;
        }

        try
        {
            var records = new List<RefreshRunRecord>(shapes.Count);

            _logger.LogInformation("Refresh cycle starting for {ShapeCount} shape(s).", shapes.Count);

            foreach (var shape in shapes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var record = await RefreshShapeAsync(shape, cancellationToken).ConfigureAwait(false);
                records.Add(record);

                // Schedule the next attempt on the grid, so a skip or a slow cycle still keeps cadence.
                ScheduleNext(shape, utcNow);
            }

            _lastRunRecords = records;

            _logger.LogInformation(
                "Refresh cycle finished. Succeeded={Succeeded}, Skipped={Skipped}, Failed={Failed}.",
                records.Count(record => record.Outcome == RefreshRunOutcome.Succeeded),
                records.Count(record => record.Outcome == RefreshRunOutcome.SkippedSourceUnreachable),
                records.Count(record => record.Outcome is not (RefreshRunOutcome.Succeeded or RefreshRunOutcome.SkippedSourceUnreachable)));

            return records;
        }
        finally
        {
            Volatile.Write(ref _cycleGate, 0);
        }
    }

    private void ScheduleNext(VisualReadShape shape, DateTime utcNow) =>
        _nextDueUtc[shape.Key] = GetNextSlotUtc(utcNow, GetInterval(shape));

    /// <summary>
    /// Returns the next run time on the schedule grid anchored at <b>local midnight</b>.
    /// </summary>
    /// <param name="utcNow">Current UTC time.</param>
    /// <param name="interval">Grid spacing; 3 hours yields 00:00/03:00/06:00/09:00/12:00/15:00/18:00/21:00 local.</param>
    /// <remarks>
    /// Anchoring to the local clock (rather than adding the interval to "whenever the last run finished")
    /// is what keeps the refresh on the times operators expect. "Server-local time" is the service host's
    /// time zone, taken from the <see cref="TimeProvider"/>.
    /// </remarks>
    private DateTime GetNextSlotUtc(DateTime utcNow, TimeSpan interval)
    {
        var localTimeZone = _timeProvider.LocalTimeZone;
        var localNow = TimeZoneInfo.ConvertTime(new DateTimeOffset(utcNow, TimeSpan.Zero), localTimeZone);

        var midnightLocal = DateTime.SpecifyKind(localNow.Date, DateTimeKind.Unspecified);
        var elapsedTicks = (localNow.DateTime - midnightLocal).Ticks;
        var slotsAhead = (elapsedTicks / interval.Ticks) + 1;
        var nextLocal = midnightLocal.AddTicks(slotsAhead * interval.Ticks);

        try
        {
            return TimeZoneInfo.ConvertTimeToUtc(nextLocal, localTimeZone);
        }
        catch (ArgumentException)
        {
            // The slot does not exist on the local clock (DST spring-forward); run at the next real time.
            return TimeZoneInfo.ConvertTimeToUtc(nextLocal.AddHours(1), localTimeZone);
        }
    }

    /// <summary>
    /// Refreshes one shape, always returning a record rather than throwing.
    /// </summary>
    /// <param name="shape">The shape to refresh.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The outcome of the attempt.</returns>
    public async Task<RefreshRunRecord> RefreshShapeAsync(
        VisualReadShape shape,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shape);

        var startedUtc = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        RefreshRunRecord record;

        try
        {
            var payload = await _payloadSource
                .BuildRefreshPayloadAsync(shape, cancellationToken)
                .ConfigureAwait(false);

            var rowCount = await _mirrorWriter
                .RefreshAsync(shape, payload, cancellationToken)
                .ConfigureAwait(false);

            stopwatch.Stop();

            record = new RefreshRunRecord
            {
                ShapeKey = shape.Key,
                StartedUtc = startedUtc,
                FinishedUtc = DateTime.UtcNow,
                Outcome = RefreshRunOutcome.Succeeded,
                RowCount = rowCount,
                DurationMs = stopwatch.ElapsedMilliseconds
            };

            _logger.LogInformation(
                "Shape {ShapeKey} refreshed: {RowCount} row(s) in {DurationMs} ms.",
                shape.Key,
                rowCount,
                stopwatch.ElapsedMilliseconds);
        }
        catch (VisualSourceUnreachableException exception)
        {
            // A skipped cycle is normal: the previous snapshot stays in place and the next cycle runs.
            stopwatch.Stop();

            record = new RefreshRunRecord
            {
                ShapeKey = shape.Key,
                StartedUtc = startedUtc,
                FinishedUtc = DateTime.UtcNow,
                Outcome = RefreshRunOutcome.SkippedSourceUnreachable,
                DurationMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = Sanitize(exception.Message)
            };

            _logger.LogWarning(
                "Shape {ShapeKey} skipped: Infor Visual unreachable; the live snapshot is unchanged. Reason: {Reason}",
                shape.Key,
                Sanitize(exception.Message));
        }
        catch (VisualSourceSchemaMismatchException exception)
        {
            // Reported as its own outcome: drift between the live read and the catalog is a design
            // fault an operator must see, not an anonymous retryable error.
            stopwatch.Stop();

            record = new RefreshRunRecord
            {
                ShapeKey = shape.Key,
                StartedUtc = startedUtc,
                FinishedUtc = DateTime.UtcNow,
                Outcome = RefreshRunOutcome.FailedSchemaMismatch,
                DurationMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = Sanitize(exception.Message)
            };

            _logger.LogError(
                "Shape {ShapeKey} failed schema validation; the live snapshot is unchanged. Reason: {Reason}",
                shape.Key,
                Sanitize(exception.Message));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            record = new RefreshRunRecord
            {
                ShapeKey = shape.Key,
                StartedUtc = startedUtc,
                FinishedUtc = DateTime.UtcNow,
                Outcome = RefreshRunOutcome.FailedUnknown,
                DurationMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = Sanitize(exception.Message)
            };

            _logger.LogError(
                exception,
                "Shape {ShapeKey} refresh failed; the live snapshot is unchanged.",
                shape.Key);
        }

        RunCompleted?.Invoke(this, record);
        return record;
    }

    /// <summary>
    /// Removes connection-string credential material from error text before it is recorded or logged.
    /// </summary>
    /// <remarks>FR-026 requires the credential never to appear in a log or status payload.</remarks>
    private static string Sanitize(string message) =>
        string.IsNullOrEmpty(message) ? message : s_secretPattern.Replace(message, "$1=***");
}
