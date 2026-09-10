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
/// <b>Scope:</b> this type runs one cycle over the catalog. The timer/scheduling loop, per-shape
/// interval overrides, and re-entrancy guarding are layered on in US3.
/// </para>
/// </remarks>
public sealed class RefreshEngine
{
    private static readonly Regex s_secretPattern = new(
        @"(password|pwd|user\s*id|uid)\s*=\s*[^;]*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly RefreshShapeCatalogProvider _catalogProvider;
    private readonly IVisualShapePayloadSource _payloadSource;
    private readonly IMockMirrorRefreshWriter _mirrorWriter;
    private readonly ILogger<RefreshEngine> _logger;

    private IReadOnlyList<RefreshRunRecord> _lastRunRecords = [];

    /// <summary>
    /// Creates the engine.
    /// </summary>
    /// <param name="catalogProvider">Supplies the validated, enabled shapes to refresh.</param>
    /// <param name="payloadSource">Produces each shape's complete result from Infor Visual.</param>
    /// <param name="mirrorWriter">Swaps each shape's new snapshot into the live mirror.</param>
    /// <param name="logger">Logger; credential material is never passed to it.</param>
    public RefreshEngine(
        RefreshShapeCatalogProvider catalogProvider,
        IVisualShapePayloadSource payloadSource,
        IMockMirrorRefreshWriter mirrorWriter,
        ILogger<RefreshEngine> logger)
    {
        ArgumentNullException.ThrowIfNull(catalogProvider);
        ArgumentNullException.ThrowIfNull(payloadSource);
        ArgumentNullException.ThrowIfNull(mirrorWriter);
        ArgumentNullException.ThrowIfNull(logger);

        _catalogProvider = catalogProvider;
        _payloadSource = payloadSource;
        _mirrorWriter = mirrorWriter;
        _logger = logger;
    }

    /// <summary>Raised after each shape attempt so the run-record store and status surface can observe it.</summary>
    public event EventHandler<RefreshRunRecord>? RunCompleted;

    /// <summary>The records produced by the most recent <see cref="RefreshCycleAsync"/> call.</summary>
    public IReadOnlyList<RefreshRunRecord> LastRunRecords => _lastRunRecords;

    /// <summary>
    /// Runs one refresh cycle over every enabled, validated shape.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One run record per refreshable shape.</returns>
    public async Task<IReadOnlyList<RefreshRunRecord>> RefreshCycleAsync(CancellationToken cancellationToken = default)
    {
        var shapes = _catalogProvider.RefreshableShapes;
        var records = new List<RefreshRunRecord>(shapes.Count);

        _logger.LogInformation("Refresh cycle starting for {ShapeCount} shape(s).", shapes.Count);

        foreach (var shape in shapes)
        {
            var record = await RefreshShapeAsync(shape, cancellationToken).ConfigureAwait(false);
            records.Add(record);
        }

        _lastRunRecords = records;

        _logger.LogInformation(
            "Refresh cycle finished. Succeeded={Succeeded}, Skipped={Skipped}, Failed={Failed}.",
            records.Count(record => record.Outcome == RefreshRunOutcome.Succeeded),
            records.Count(record => record.Outcome == RefreshRunOutcome.SkippedSourceUnreachable),
            records.Count(record => record.Outcome is not (RefreshRunOutcome.Succeeded or RefreshRunOutcome.SkippedSourceUnreachable)));

        return records;
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
