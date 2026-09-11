using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Services;

/// <summary>
/// Tracks the read state the shell indicator renders, and how old the served cached data is.
/// </summary>
/// <remarks>
/// <para>
/// The state comes only from the detector, so nothing in the application can force it: there is no mode to
/// set, and no public API here changes <see cref="Current"/> (FR-003, FR-005). The provider is a pure
/// observer — it subscribes to the detector and republishes its state with the freshness that belongs to it.
/// </para>
/// <para>
/// <b>Age is informational only.</b> Data is never refused because it is old; the age exists so an operator
/// can see how stale the cache is, and seed-only content is reported as such rather than as an age
/// (FR-017, FR-022).
/// </para>
/// </remarks>
public sealed class ReadStatusProvider : IReadStatusProvider
{
    private readonly IVisualReachabilityDetector _detector;
    private readonly IVisualShapeFreshnessReader? _freshnessReader;
    private readonly TimeProvider _timeProvider;
    private readonly object _gate = new();

    private ReadStatusSnapshot _current;

    /// <summary>Creates the provider.</summary>
    /// <param name="detector">The reachability detector whose state is published.</param>
    /// <param name="freshnessReader">
    /// Supplies cached-data age. Optional so the provider still works when the cache is not configured — the
    /// age is then unknown rather than wrong.
    /// </param>
    /// <param name="timeProvider">Time source for the age calculation.</param>
    public ReadStatusProvider(
        IVisualReachabilityDetector detector,
        IVisualShapeFreshnessReader? freshnessReader = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(detector);

        _detector = detector;
        _freshnessReader = freshnessReader;
        _timeProvider = timeProvider ?? TimeProvider.System;

        _current = new ReadStatusSnapshot { Status = detector.Current };

        _detector.StateChanged += OnDetectorStateChanged;
    }

    /// <inheritdoc />
    public ReadStatusSnapshot Current
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
    public event EventHandler<ReadStatusSnapshot>? Changed;

    /// <summary>
    /// Refreshes the reported state from the detector and the cache's freshness.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// Called by the probe host after each probe, so the indicator's age is re-evaluated as time passes even
    /// while the state itself has not changed.
    /// </remarks>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var status = _detector.Current;
        var snapshot = new ReadStatusSnapshot { Status = status };

        if (_freshnessReader is not null)
        {
            try
            {
                var freshness = await _freshnessReader
                    .GetFreshnessAsync(shapeKey: null, cancellationToken)
                    .ConfigureAwait(false);

                var (lastRefreshUtc, isSeedOnly) = Summarize(freshness);

                snapshot = snapshot with
                {
                    IsSeedContentOnly = isSeedOnly,
                    CachedDataAgeUtc = isSeedOnly || lastRefreshUtc is null
                        ? null
                        : _timeProvider.GetUtcNow().UtcDateTime - lastRefreshUtc.Value,
                    PerShapeLastRefreshUtc = freshness
                        .Where(entry => entry.RefreshedUtc.HasValue)
                        .ToDictionary(
                            entry => entry.ShapeKey,
                            entry => new DateTimeOffset(entry.RefreshedUtc!.Value, TimeSpan.Zero),
                            StringComparer.Ordinal)
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                // An unreadable cache must not make the state unavailable: the state itself is known, and the
                // age simply stays unknown rather than being invented.
                snapshot = snapshot with { CachedDataAgeUtc = null };
            }
        }

        Publish(snapshot);
    }

    private void OnDetectorStateChanged(object? sender, VisualReadStatus status)
    {
        Publish(Current with { Status = status });
    }

    private void Publish(ReadStatusSnapshot snapshot)
    {
        bool changed;

        lock (_gate)
        {
            changed = _current != snapshot;
            _current = snapshot;
        }

        if (changed)
        {
            Changed?.Invoke(this, snapshot);
        }
    }

    /// <summary>
    /// Reduces per-shape freshness to the single age and seed-only flag the indicator states.
    /// </summary>
    /// <param name="freshness">The per-shape entries.</param>
    /// <returns>The most recent refresh time and whether every shape is still seed-only.</returns>
    internal static (DateTime? LastRefreshedUtc, bool IsSeedContentOnly) Summarize(
        IReadOnlyList<VisualShapeFreshness> freshness)
    {
        ArgumentNullException.ThrowIfNull(freshness);

        var refreshes = freshness
            .Where(entry => entry.RefreshedUtc.HasValue)
            .Select(entry => entry.RefreshedUtc!.Value)
            .ToList();

        return (
            refreshes.Count == 0 ? null : refreshes.Max(),
            freshness.Count == 0 || freshness.All(entry => entry.IsSeedContentOnly));
    }
}
