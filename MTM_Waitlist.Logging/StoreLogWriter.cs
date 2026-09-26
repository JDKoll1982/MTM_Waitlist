using System.Threading.Channels;

using Microsoft.Extensions.Hosting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Logging;

/// <summary>
/// The queue between the seam and the store, and the host's bounded shutdown flush (FR-037, plan D20, D27).
/// </summary>
/// <remarks>
/// <para>
/// <b>A caller is never delayed.</b> <see cref="Enqueue"/> hands the record to a bounded in-memory queue and
/// returns; a single reader writes to the store on a background task. A slow store therefore slows the writer
/// and nobody else — which matters because a slow store is one of the faults the log exists to record.
/// </para>
/// <para>
/// <b>A full queue drops its oldest entry rather than blocking or growing.</b> The newest faults are the ones
/// worth keeping, and a queue that could grow without limit would be a memory leak in exactly the run that is
/// already in trouble.
/// </para>
/// <para>
/// <b>Nothing is written to the machine.</b> There is no disk queue, no local log file and no retry journal. An
/// entry the store refuses is dropped, and the application keeps no substitute record (FR-025, plan D27).
/// </para>
/// <para>
/// <b>The writer absorbs its own failures.</b> A store that throws, or a record that cannot be turned into
/// parameters, is dropped without a second diagnostic: this type records nothing about itself and takes no
/// logger, because routing its own failure back into the seam it serves is how one broken write becomes a fault
/// that produces a fault (SC-016). It never throws to its caller either.
/// </para>
/// <para>
/// <b>Order is the queue's order.</b> One reader takes records first in, first out, so the store's hash chain
/// reads as a history of what the application did.
/// </para>
/// </remarks>
public sealed class StoreLogWriter : IHostedService
{
    /// <summary>The queue's default capacity, in entries.</summary>
    /// <remarks>
    /// Large enough that a burst of diagnostics during a failed launch is kept whole, and small enough that the
    /// queue is a bounded amount of memory even when the store never answers.
    /// </remarks>
    public const int DefaultQueueCapacity = 4096;

    /// <summary>
    /// The default ceiling on a flush, as a stated maximum rather than an open wait.
    /// </summary>
    /// <remarks>
    /// A dialog-free five seconds: long enough for a healthy store to take what is queued, short enough that a
    /// stalled one cannot hold the process open past the point where the person has already closed the window. It
    /// sits well inside the thirty-second ceiling D21 states for a launch-path wait, because a shutdown flush is
    /// not a step of the launch and must not behave like one.
    /// </remarks>
    public static readonly TimeSpan DefaultFlushTimeLimit = TimeSpan.FromSeconds(5);

    /// <summary>The procedure that carries the write. It is the store's single writer (contract §3).</summary>
    internal const string InsertProcedureName = "sp_ops_startup_logs_insert";

    /// <summary>The database the log store lives in.</summary>
    internal const string DatabaseName = "mtm_waitlist";

    private readonly IMySqlHelperServer _store;
    private readonly Channel<StoreLogRecord> _queue;
    private readonly TimeSpan _flushTimeLimit;
    private readonly CancellationTokenSource _stopping = new();
    private readonly object _drainGate = new();

    private TaskCompletionSource _idle = NewIdleSignal();
    private Task? _drain;
    private int _inFlight;

    /// <summary>
    /// Creates the writer.
    /// </summary>
    /// <param name="store">The stored-procedure seam the write goes through.</param>
    /// <param name="queueCapacity">The queue's capacity; the default is <see cref="DefaultQueueCapacity"/>.</param>
    /// <param name="flushTimeLimit">The flush ceiling; the default is <see cref="DefaultFlushTimeLimit"/>.</param>
    public StoreLogWriter(
        IMySqlHelperServer store,
        int queueCapacity = DefaultQueueCapacity,
        TimeSpan? flushTimeLimit = null)
    {
        ArgumentNullException.ThrowIfNull(store);

        _store = store;
        _flushTimeLimit = flushTimeLimit ?? DefaultFlushTimeLimit;

        if (_flushTimeLimit <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(flushTimeLimit), _flushTimeLimit, "A flush must be given a positive ceiling.");
        }

        _queue = Channel.CreateBounded<StoreLogRecord>(new BoundedChannelOptions(Math.Max(1, queueCapacity))
        {
            SingleReader = true,
            SingleWriter = false,

            // The drop policy is the queue's, not the writer's: it is applied atomically when the queue is full,
            // so no entry is ever lost while a caller computes which one to discard.
            FullMode = BoundedChannelFullMode.DropOldest,
        });
    }

    /// <summary>
    /// Queues one record. It never blocks and never throws.
    /// </summary>
    /// <param name="record">The composed record.</param>
    public void Enqueue(StoreLogRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        EnsureDraining();

        // A bounded queue in its drop-oldest mode always accepts a write while it is open, so the answer needs no
        // branch: a full queue has already made room by discarding its oldest entry.
        _queue.Writer.TryWrite(record);
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        EnsureDraining();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Waits, within the flush ceiling, for the entries already queued to reach the store.
    /// </summary>
    /// <param name="cancellationToken">The caller's cancellation.</param>
    /// <returns>A task that completes when nothing is left to write, at the ceiling, or on cancellation.</returns>
    /// <remarks>
    /// This is the only member a caller can await, and it never throws: it returns whether the store answered or
    /// not, because a shutdown that reported a logging failure would be reporting it to nobody.
    /// </remarks>
    public async Task FlushAsync(CancellationToken cancellationToken)
    {
        if (_queue.Reader.Count == 0 && Volatile.Read(ref _inFlight) == 0)
        {
            return;
        }

        try
        {
            var idle = Volatile.Read(ref _idle).Task;
            await Task.WhenAny(idle, Task.Delay(_flushTimeLimit, cancellationToken)).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // The flush is over either way; what is queued is written by the drain if it can be.
        }
        catch (Exception)
        {
            // Bounded and best effort by construction: nothing here is worth raising at the caller.
        }
    }

    /// <summary>
    /// Stops accepting new records and gives what is queued a bounded chance to reach the store.
    /// </summary>
    /// <param name="cancellationToken">The host's shutdown cancellation.</param>
    /// <returns>A task that completes when the flush ceiling is reached or the queue empties.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // Completing the writer stops new records being accepted; the drain keeps reading what is already queued.
        _queue.Writer.TryComplete();

        var drain = Volatile.Read(ref _drain);

        try
        {
            if (drain is not null)
            {
                await Task.WhenAny(drain, Task.Delay(_flushTimeLimit, cancellationToken)).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // The bounded flush is over; the drain is cancelled below either way.
        }
        finally
        {
            // Cancels a write that outlasted its ceiling, so a stalled store cannot hold the process open. A
            // record dropped here is lost rather than deferred, which is the cost the specification states.
            _stopping.Cancel();
        }
    }

    private void EnsureDraining()
    {
        if (Volatile.Read(ref _drain) is not null)
        {
            return;
        }

        lock (_drainGate)
        {
            _drain ??= Task.Run(() => DrainAsync(_stopping.Token), CancellationToken.None);
        }
    }

    private async Task DrainAsync(CancellationToken token)
    {
        try
        {
            while (await _queue.Reader.WaitToReadAsync(token).ConfigureAwait(false))
            {
                while (_queue.Reader.TryRead(out var record))
                {
                    Interlocked.Increment(ref _inFlight);

                    try
                    {
                        await WriteToStoreAsync(record, token).ConfigureAwait(false);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _inFlight);
                    }
                }

                MarkIdle();
            }
        }
        catch (OperationCanceledException)
        {
            // The shutdown ceiling was reached with work still queued; the process is ending, so the records are
            // dropped rather than held.
        }
        catch (ChannelClosedException)
        {
            // The writer was completed while the reader was waiting; every queued record has been taken.
        }
        catch (Exception)
        {
            // Absorbed: the writer records nothing about its own failure, so no second diagnostic can exist.
        }
        finally
        {
            MarkIdle();
        }
    }

    private async Task WriteToStoreAsync(StoreLogRecord record, CancellationToken token)
    {
        try
        {
            await _store
                .ExecuteStoredProcedureNonQueryAsync(
                    InsertProcedureName,
                    ToParameters(record),
                    MySqlDatabaseTarget.MtmWaitlist,
                    token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Shutdown, not a fault worth describing.
        }
        catch (Exception)
        {
            // A store that refused the write drops the entry. Nothing is written to the machine in its place and
            // nothing is said about the refusal: this is the clause that keeps one broken write from producing a
            // fault that produces a fault (FR-037, SC-016).
        }
    }

    private void MarkIdle()
    {
        // The signal is replaced before it is completed, so a flush that starts after this point waits on the
        // next idle rather than on an already-completed one.
        var previous = Interlocked.Exchange(ref _idle, NewIdleSignal());
        previous.TrySetResult();
    }

    private static TaskCompletionSource NewIdleSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static IReadOnlyDictionary<string, object?> ToParameters(StoreLogRecord record) =>
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["p_public_id"] = record.PublicId,
            ["p_correlation_id"] = record.CorrelationId,
            ["p_level"] = record.Level,
            ["p_event_action"] = record.EventAction,
            ["p_outcome"] = record.Outcome,
            ["p_actor_kind"] = record.ActorKind,
            ["p_actor_id"] = record.ActorId,
            ["p_host_id"] = record.HostId,
            ["p_mac_address"] = record.MacAddress,
            ["p_module"] = record.Module,
            ["p_error_type"] = record.ErrorType,
            ["p_message"] = record.Message,
            ["p_exception_detail"] = record.ExceptionDetail,
            ["p_error_fingerprint"] = record.ErrorFingerprint,
            ["p_payload_json"] = record.PayloadJson,
        };
}
