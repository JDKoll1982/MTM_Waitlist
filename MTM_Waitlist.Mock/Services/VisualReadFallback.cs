using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Mock.Services;

/// <summary>
/// The shared live-then-cached read algorithm every read shape uses.
/// </summary>
/// <typeparam name="TRequest">The shape's input.</typeparam>
/// <typeparam name="TRow">The shape's single row type.</typeparam>
/// <remarks>
/// <para>
/// Implemented once here so the five shapes cannot drift apart (FR-024, SC-004). A concrete shape
/// supplies only what differs: its catalog definition, its live parameters, its cache parameters, and
/// its row mapping.
/// </para>
/// <para>
/// The three outcomes are treated as genuinely different, which is the whole point of the classified
/// executor: a successful live read is returned <em>including an empty set</em>, only unreachability
/// reads the mirror, and every other failure surfaces. That is what stops the cache from masking a real
/// fault or from replacing a legitimately empty answer.
/// </para>
/// <para>
/// <b>A settled verdict is reused.</b> When the reachability probe has established that Infor Visual is
/// unreachable, the live attempt is skipped and the mirror answers directly — because the attempt could only
/// rediscover the verdict, at the cost of a connect timeout, on every read. The read path never decides
/// unreachability from its own failure; it only ever honours a verdict the probe reached, and the probe loop
/// is what keeps asking and therefore what detects recovery.
/// </para>
/// </remarks>
public abstract class VisualReadFallback<TRequest, TRow> : IVisualReadFallback<TRequest, TRow>
{
    private readonly IVisualQueryExecutor _executor;
    private readonly IMySqlHelperServer? _mySqlHelperServer;
    private readonly IVisualReachabilityDetector? _reachability;

    /// <summary>Creates the fallback.</summary>
    /// <param name="executor">Runs the live read against Infor Visual.</param>
    /// <param name="mySqlHelperServer">
    /// Reads the <c>mtm_mock</c> mirror. Optional so the shape can still be exercised without a cache
    /// configured; an unreachable source then surfaces instead of silently returning nothing.
    /// </param>
    /// <param name="reachability">
    /// Supplies the settled reachability verdict. Optional, and when it is absent every read attempts the live
    /// source exactly as it always did — the short-circuit optimises a fact the probe has already established,
    /// it is never a second source of truth.
    /// </param>
    protected VisualReadFallback(
        IVisualQueryExecutor executor,
        IMySqlHelperServer? mySqlHelperServer,
        IVisualReachabilityDetector? reachability = null)
    {
        ArgumentNullException.ThrowIfNull(executor);

        _executor = executor;
        _mySqlHelperServer = mySqlHelperServer;
        _reachability = reachability;
    }

    /// <summary>
    /// Whether the probe has already settled on "Infor Visual is unreachable", making a live attempt a way of
    /// rediscovering it at the cost of a connect timeout.
    /// </summary>
    private bool IsVerdictCached => _reachability?.Current == VisualReadStatus.Cached;

    /// <summary>The catalog definition this fallback serves.</summary>
    protected abstract VisualReadShape Shape { get; }

    /// <summary>Builds the parameters for the live read, named as the source script declares them.</summary>
    protected abstract IReadOnlyDictionary<string, object?> BuildLiveParameters(TRequest request);

    /// <summary>Builds the parameters for <c>sp_visual_&lt;shape&gt;_get</c>, in its own naming scheme.</summary>
    protected abstract IReadOnlyDictionary<string, object?> BuildCacheParameters(TRequest request);

    /// <summary>Maps one raw provider row onto the shape's row type.</summary>
    protected abstract TRow MapRow(IReadOnlyDictionary<string, object?> row);

    /// <summary>
    /// Resolves a shape from the shipped catalog, failing loudly when a key is not registered rather
    /// than leaving a shape that silently serves nothing.
    /// </summary>
    protected static VisualReadShape ResolveShape(string shapeKey) =>
        VisualReadShapeCatalog.FindByKey(shapeKey)
        ?? throw new InvalidOperationException($"Read shape '{shapeKey}' is not registered in the shape catalog.");

    /// <inheritdoc />
    public async Task<IReadOnlyList<TRow>> ReadAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var result = await ReadWithProvenanceAsync(request, cancellationToken).ConfigureAwait(false);
        return result.Value;
    }

    /// <inheritdoc />
    public async Task<CachedReadResult<IReadOnlyList<TRow>>> ReadWithProvenanceAsync(
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        // A settled "unreachable" verdict is reused rather than re-established on every read. The probe already
        // knows the answer — it establishes it in two seconds, while a read was spending its own full connect
        // timeout rediscovering the same fact — and the probe loop, not the read path, is what keeps asking and
        // therefore what detects recovery. That keeps the promise that matters: cached rows are served only
        // while Infor Visual is unreachable, and never merely because a read once found it so.
        if (IsVerdictCached)
        {
            return await ReadFromMirrorAsync(request, cancellationToken).ConfigureAwait(false);
        }

        var outcome = await _executor
            .ExecuteAsync(Shape.SourceScriptRelativePath, BuildLiveParameters(request), cancellationToken)
            .ConfigureAwait(false);

        switch (outcome.Status)
        {
            case VisualQueryStatus.Ok:
                // An empty live result is a valid answer and must never be replaced by cached rows.
                return new CachedReadResult<IReadOnlyList<TRow>>
                {
                    Value = MapRows(outcome.Rows),
                    Source = VisualReadSource.ServedFromLive,
                };

            case VisualQueryStatus.Unreachable:
                return await ReadFromMirrorAsync(request, cancellationToken).ConfigureAwait(false);

            default:
                throw new VisualReadFailedException(
                    Shape.Key,
                    outcome.Reason ?? $"The Infor Visual read for shape '{Shape.Key}' failed.");
        }
    }

    private async Task<CachedReadResult<IReadOnlyList<TRow>>> ReadFromMirrorAsync(
        TRequest request,
        CancellationToken cancellationToken)
    {
        if (_mySqlHelperServer is null)
        {
            // Returning an empty set here would be indistinguishable from a real empty answer and would
            // quietly lose the caller's data, so the condition surfaces instead.
            throw new VisualReadFailedException(
                Shape.Key,
                $"Infor Visual is unreachable and no mirror cache is configured for shape '{Shape.Key}'.");
        }

        var rows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            Shape.GetProcedureName,
            BuildCacheParameters(request),
            MySqlDatabaseTarget.MtmMock,
            cancellationToken).ConfigureAwait(false);

        return new CachedReadResult<IReadOnlyList<TRow>>
        {
            Value = MapRows(rows),
            Source = VisualReadSource.ServedFromCache,
            // The mirror's refreshed_utc is deliberately not projected by sp_visual_<shape>_get: adding a
            // column would break the structural identity this fallback guarantees, so snapshot age is
            // surfaced through the read-status surface instead.
            RefreshedUtc = null,
        };
    }

    private TRow[] MapRows(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var mapped = new TRow[rows.Count];
        for (var index = 0; index < rows.Count; index++)
        {
            mapped[index] = MapRow(rows[index]);
        }

        return mapped;
    }
}
