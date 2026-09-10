using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Services;

/// <summary>
/// Tracks Infor Visual reachability by probing, with hysteresis to damp flapping.
/// </summary>
/// <remarks>
/// <para>
/// The state is derived only from probe results, so nothing can force it and there is no manual mode
/// (FR-003). Two consecutive failures are required to accept that the source is down, while a single
/// success returns to live — so a momentary blip does not flip the whole application to cached reads,
/// and recovery is immediate (FR-002, research R11).
/// </para>
/// <para>
/// This type deliberately references nothing from the retired mock-routing stack: no stored toggle, no
/// configuration service, and no mode-change detector (FR-014). Scheduling and any backoff between
/// probes belong to the host that calls <see cref="ProbeAsync"/>.
/// </para>
/// </remarks>
public sealed class VisualReachabilityDetector : IVisualReachabilityDetector
{
    private const int FailuresBeforeCached = 2;

    private readonly IVisualConnectivityProbe _probe;
    private readonly object _gate = new();

    private int _consecutiveFailures;
    private VisualReadStatus _current = VisualReadStatus.Unknown;

    /// <summary>Creates the detector.</summary>
    /// <param name="probe">The lightweight connectivity probe to run.</param>
    public VisualReachabilityDetector(IVisualConnectivityProbe probe)
    {
        ArgumentNullException.ThrowIfNull(probe);
        _probe = probe;
    }

    /// <inheritdoc />
    public VisualReadStatus Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<VisualReadStatus>? StateChanged;

    /// <inheritdoc />
    public async Task ProbeAsync(CancellationToken cancellationToken = default)
    {
        var reachable = await _probe.ProbeAsync(cancellationToken).ConfigureAwait(false);

        VisualReadStatus? newStatus = null;
        lock (_gate)
        {
            if (reachable)
            {
                _consecutiveFailures = 0;
                if (_current != VisualReadStatus.Live)
                {
                    _current = VisualReadStatus.Live;
                    newStatus = _current;
                }
            }
            else
            {
                _consecutiveFailures++;
                if (_consecutiveFailures >= FailuresBeforeCached && _current != VisualReadStatus.Cached)
                {
                    _current = VisualReadStatus.Cached;
                    newStatus = _current;
                }
            }
        }

        if (newStatus.HasValue)
        {
            StateChanged?.Invoke(this, newStatus.Value);
        }
    }
}
