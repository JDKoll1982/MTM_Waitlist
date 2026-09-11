using Microsoft.Extensions.Logging;
using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// Persists every completed shape refresh into the durable run-record store.
/// </summary>
/// <remarks>
/// <para>
/// <b>This subscription is the whole point of the type.</b> <see cref="RefreshEngine.RunCompleted"/> carries
/// each shape's outcome, but an event with no subscriber records nothing: without this, the store is loaded,
/// read back by the API and the status surface, and never written — so every shape reports "Never" no matter
/// how many cycles have run, and FR-013's per-item last-run outcome and timestamp are unmet.
/// </para>
/// <para>
/// It subscribes on construction so the wiring cannot be forgotten at a call site, and it records for every
/// path that runs a shape — scheduled, on-demand, or manual — because all three raise the same event.
/// </para>
/// <para>
/// Recording is best-effort: a durable-store failure is logged and swallowed, because losing an operational
/// record must never fail the refresh that just succeeded, and the cycle's own outcome is already reported.
/// </para>
/// </remarks>
public sealed class RefreshRunRecordRecorder : IDisposable
{
    private readonly RefreshEngine _engine;
    private readonly RefreshRunRecordStore _store;
    private readonly ILogger<RefreshRunRecordRecorder> _logger;

    /// <summary>Creates the recorder and subscribes to the engine.</summary>
    /// <param name="engine">The engine whose completed runs are recorded.</param>
    /// <param name="store">The durable store the records are written to.</param>
    /// <param name="logger">Logger; error text is sanitized by the store, which never sees a credential.</param>
    public RefreshRunRecordRecorder(
        RefreshEngine engine,
        RefreshRunRecordStore store,
        ILogger<RefreshRunRecordRecorder> logger)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(logger);

        _engine = engine;
        _store = store;
        _logger = logger;

        _engine.RunCompleted += OnRunCompleted;
    }

    /// <summary>How many records this recorder has persisted, for diagnostics and tests.</summary>
    public int RecordedCount { get; private set; }

    /// <inheritdoc />
    /// <remarks>
    /// Disposal is synchronous on purpose: unsubscribing is a synchronous operation, and an async-only
    /// disposable registered in the container would make the container's own <c>Dispose()</c> throw at
    /// shutdown.
    /// </remarks>
    public void Dispose() => _engine.RunCompleted -= OnRunCompleted;

    private void OnRunCompleted(object? sender, RefreshRunRecord record)
    {
        // Fire-and-forget by necessity: the engine raises this synchronously from the refresh path, and the
        // store's write must not be able to stall the cycle.
        _ = RecordAsync(record);
    }

    private async Task RecordAsync(RefreshRunRecord record)
    {
        try
        {
            await _store.RecordAsync(record).ConfigureAwait(false);
            RecordedCount++;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "The run record for shape {ShapeKey} could not be persisted; the refresh outcome itself is unaffected.",
                record.ShapeKey);
        }
    }
}
