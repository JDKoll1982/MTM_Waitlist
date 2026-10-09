using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Module_Logging;

/// <summary>
/// The store seam, scripted: it records every write attempt, can be held inside one write, and can refuse every
/// write.
/// </summary>
/// <remarks>
/// <para>
/// The logging seam and its writer are about behaviour a live store cannot demonstrate — a caller that must not
/// wait, a queue that must drop its oldest entry, a refusal that must not become a second diagnostic. Holding the
/// store inside a write is what makes those deterministic rather than timing-dependent: the test releases the
/// gate when it has finished asserting, so nothing depends on a clock.
/// </para>
/// <para>
/// A write is recorded <b>before</b> the gate is awaited, so "what reached the store" includes the write the
/// store is currently stuck inside — which is what lets a drop-oldest assertion name the exact entries kept.
/// </para>
/// </remarks>
internal sealed class RecordingLogStore : IMySqlHelperServer
{
    /// <summary>How long a test waits for the writer to be inside its first write before failing.</summary>
    private static readonly TimeSpan DefaultFirstWriteWait = TimeSpan.FromSeconds(10);

    private readonly TaskCompletionSource _firstWriteStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private TaskCompletionSource _gate = Released();

    /// <summary>Every write attempt, in the order the writer made them.</summary>
    public List<RecordedStoreWrite> Writes { get; } = [];

    /// <summary>When set, every write throws it. Used to prove a refusal is absorbed.</summary>
    public Exception? WriteFailure { get; set; }

    /// <summary>
    /// Waits, within a stated limit, for the writer to be inside its first write.
    /// </summary>
    /// <param name="timeout">How long to wait; ten seconds when none is named.</param>
    /// <returns>A task that completes once the first write has started.</returns>
    /// <exception cref="TimeoutException">No write reached the store within the limit.</exception>
    /// <remarks>
    /// <b>A bare wait on the first write is a way to hang the whole run.</b> When nothing reaches the store —
    /// because the entry was dropped before it was queued, or the writer never drained — that wait never
    /// completes: the test never returns, the run produces no summary and names nothing, and the test host it
    /// leaves behind holds the test assembly so every later run waits on that lock too. MSTest cannot abort a
    /// non-cooperative async test, so nothing upstream rescues it. This turns that silence into a failure that
    /// says what did not happen.
    /// </remarks>
    public async Task WaitForFirstWriteAsync(TimeSpan? timeout = null)
    {
        var limit = timeout ?? DefaultFirstWriteWait;
        var finished = await Task.WhenAny(_firstWriteStarted.Task, Task.Delay(limit)).ConfigureAwait(false);

        if (!ReferenceEquals(finished, _firstWriteStarted.Task))
        {
            throw new TimeoutException(
                $"No write reached the store within {limit.TotalSeconds:0.#}s. The entry was most likely dropped "
                + "before it was queued (a severity below the store's threshold), so the writer had nothing to drain.");
        }
    }

    /// <summary>Whether the writer is currently held inside a write.</summary>
    public bool IsBlocked => !Volatile.Read(ref _gate).Task.IsCompleted;

    /// <summary>The messages that reached the store, in order.</summary>
    public IReadOnlyList<string?> Messages => [.. Writes.Select(write => write.Message)];

    /// <summary>Holds the writer inside its next write until <see cref="ReleaseWrites"/> is called.</summary>
    public void BlockWrites() => Volatile.Write(ref _gate, Held());

    /// <summary>Lets the held write — and every write after it — finish.</summary>
    public void ReleaseWrites() => Volatile.Read(ref _gate).TrySetResult();

    public async Task<int> ExecuteStoredProcedureNonQueryAsync(
        string storedProcedureName,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default)
    {
        Writes.Add(new RecordedStoreWrite(storedProcedureName, databaseTarget, parameters));
        _firstWriteStarted.TrySetResult();

        var gate = Volatile.Read(ref _gate);
        await gate.Task.ConfigureAwait(false);

        if (WriteFailure is not null)
        {
            throw WriteFailure;
        }

        return 1;
    }

    public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
        string storedProcedureName,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(Array.Empty<Dictionary<string, object?>>());

    public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
        string sql,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(Array.Empty<Dictionary<string, object?>>());

    public Task<int> ExecuteSqlNonQueryAsync(
        string sql,
        IReadOnlyDictionary<string, object?> parameters,
        MySqlDatabaseTarget databaseTarget,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    private static TaskCompletionSource Released()
    {
        var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        released.TrySetResult();
        return released;
    }

    private static TaskCompletionSource Held() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>One write attempt: the procedure, the target, and the parameters the writer handed over.</summary>
/// <param name="Procedure">The stored procedure named by the writer.</param>
/// <param name="DatabaseTarget">The store the write was aimed at.</param>
/// <param name="Parameters">The parameters, keyed by their procedure parameter names.</param>
internal sealed record RecordedStoreWrite(
    string Procedure,
    MySqlDatabaseTarget DatabaseTarget,
    IReadOnlyDictionary<string, object?> Parameters)
{
    /// <summary>The entry's message, which is the parameter every test uses to name an entry.</summary>
    public string? Message => Parameters.TryGetValue("p_message", out var value) ? value as string : null;

    /// <summary>Reads one parameter as a string, so an assertion does not have to cast at each call site.</summary>
    public string? Value(string parameterName) =>
        Parameters.TryGetValue(parameterName, out var value) ? value as string : null;
}
