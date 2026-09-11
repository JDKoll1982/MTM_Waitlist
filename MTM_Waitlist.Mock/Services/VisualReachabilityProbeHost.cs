using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Services;

/// <summary>
/// Runs the reachability probe on a schedule, with backoff while the source is unreachable.
/// </summary>
/// <remarks>
/// <para>
/// <b>Probing only.</b> This host never refreshes the cache: the application is not allowed to schedule
/// refreshes (FR-025), and the mirror is filled by the on-host service. Its single job is to keep the
/// detector's state honest so the read-status indicator reflects reality (FR-005).
/// </para>
/// <para>
/// <b>The probe cadence is not the refresh cadence.</b> The mirror is refreshed by the service on its own
/// schedule — three-hourly by default — and nothing here changes, triggers, or shortens that. This loop only
/// asks "can Infor Visual be reached right now?", which is why it runs on its own short interval.
/// </para>
/// <para>
/// <b>Intervals</b>: the live interval applies while the state is <see cref="VisualReadStatus.Live"/> or
/// <see cref="VisualReadStatus.Unknown"/> — before a state exists, probing must continue at the live cadence
/// so two failures can be observed promptly and the indicator can appear; only once cached does the loop back
/// off, so an outage does not turn into a poll loop against a dead server. The first probe runs immediately,
/// so a freshly started application does not sit at <c>Unknown</c> until the first interval elapses.
/// </para>
/// <para>
/// The loop is fault-tolerant by construction: a probe that throws is logged as a failure and the loop
/// continues, because losing reachability visibility must never take the application down.
/// </para>
/// </remarks>
public sealed class VisualReachabilityProbeHost : IVisualReachabilityProbeHost
{
    /// <summary>Probe interval while Infor Visual is reachable.</summary>
    public static readonly TimeSpan LiveProbeInterval = TimeSpan.FromSeconds(30);

    /// <summary>Probe interval while cached data is being served.</summary>
    public static readonly TimeSpan CachedProbeInterval = TimeSpan.FromMinutes(5);

    private readonly IVisualReachabilityDetector _detector;
    private readonly ReadStatusProvider? _statusProvider;
    private readonly TimeSpan _liveInterval;
    private readonly TimeSpan _cachedInterval;

    private Task? _loop;
    private CancellationTokenSource? _loopSource;

    /// <summary>Creates the host.</summary>
    /// <param name="detector">The detector to probe.</param>
    /// <param name="statusProvider">
    /// Optional status provider, refreshed after every probe so the reported cached-data age keeps up with
    /// the clock even while the state itself has not changed.
    /// </param>
    /// <param name="liveInterval">Probe interval while reachable; defaults to 30 seconds.</param>
    /// <param name="cachedInterval">Probe interval while cached; defaults to 5 minutes.</param>
    public VisualReachabilityProbeHost(
        IVisualReachabilityDetector detector,
        ReadStatusProvider? statusProvider = null,
        TimeSpan? liveInterval = null,
        TimeSpan? cachedInterval = null)
    {
        ArgumentNullException.ThrowIfNull(detector);

        _detector = detector;
        _statusProvider = statusProvider;
        _liveInterval = liveInterval ?? LiveProbeInterval;
        _cachedInterval = cachedInterval ?? CachedProbeInterval;

        if (_liveInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(liveInterval), _liveInterval, "The live probe interval must be greater than zero.");
        }

        if (_cachedInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(cachedInterval), _cachedInterval, "The cached probe interval must be greater than zero.");
        }
    }

    /// <inheritdoc />
    public bool IsRunning => _loop is { IsCompleted: false };

    /// <inheritdoc />
    public void Start(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            return;
        }

        _loopSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _loopSource.Token;

        _loop = Task.Run(() => RunAsync(token), CancellationToken.None);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_loopSource is not null)
        {
            await _loopSource.CancelAsync().ConfigureAwait(false);
        }

        if (_loop is not null)
        {
            try
            {
                await _loop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        _loopSource?.Dispose();
        _loopSource = null;
        _loop = null;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _detector.ProbeAsync(cancellationToken).ConfigureAwait(false);

                if (_statusProvider is not null)
                {
                    await _statusProvider.RefreshAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception)
            {
                // A failed probe is exactly what the detector records as a failure; the loop must survive it.
            }

            try
            {
                // Back off only while cached. `Unknown` must keep the live cadence: it is the state the very
                // first probes run in, and treating it as "cached" would delay the two-failure transition (and
                // therefore the indicator) by a whole cached interval.
                var interval = _detector.Current == VisualReadStatus.Cached ? _cachedInterval : _liveInterval;
                await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
