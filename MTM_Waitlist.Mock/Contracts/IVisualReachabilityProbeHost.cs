namespace MTM_Waitlist.Mock.Contracts;

/// <summary>
/// Drives the reachability probe on a schedule so the read state can actually change (FR-005, US4).
/// </summary>
/// <remarks>
/// <para>
/// Without a driver the detector would stay <c>Unknown</c> forever — nothing else calls
/// <see cref="IVisualReachabilityDetector.ProbeAsync"/> — and the shell indicator could never appear. This
/// host is deliberately <b>app-side and probing-only</b>: refresh scheduling stays owned by the on-host
/// service, because the application must never perform a scheduled refresh (FR-025).
/// </para>
/// <para>
/// While the source is reachable the host probes on a short interval, so recovery is noticed promptly; while
/// it is cached it backs off, so an outage does not hammer a dead server.
/// </para>
/// </remarks>
public interface IVisualReachabilityProbeHost : IAsyncDisposable
{
    /// <summary>Whether the probe loop is running.</summary>
    bool IsRunning { get; }

    /// <summary>
    /// Starts the probe loop. Calling it twice is a no-op.
    /// </summary>
    /// <param name="cancellationToken">Stops the loop.</param>
    void Start(CancellationToken cancellationToken = default);
}
