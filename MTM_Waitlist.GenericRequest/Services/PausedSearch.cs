namespace MTM_Waitlist.GenericRequest.Services;

/// <summary>
/// One read per typing pause, and none while the requester is still typing.
/// </summary>
/// <remarks>
/// <para>
/// Every keystroke restarts the pause and abandons the one before it, so a ten-character part number is one read when
/// it is typed without stopping and two when the requester pauses in the middle — never ten. The abandoned pause also
/// cancels its read, so a read nobody is waiting for stops asking (PERF-001).
/// </para>
/// <para>
/// Cancelling is not the whole story: a read the plant has already answered comes back as an answer rather than a
/// cancellation. Each pause carries the number it was given, the read is handed that number, and the reader applies an
/// answer only while it is still the newest — so an older answer is discarded by its own identity, never by whichever
/// happens to arrive last.
/// </para>
/// </remarks>
public sealed class PausedSearch : IDisposable
{
    private readonly TimeSpan _pause;
    private readonly Func<TimeSpan, CancellationToken, Task> _wait;

    private CancellationTokenSource? _pending;
    private long _attempt;

    /// <summary>Creates the pause.</summary>
    /// <param name="pause">How long typing has to stop before the read is asked for.</param>
    /// <param name="wait">
    /// Waits out the pause. Defaults to a real delay; a test supplies its own so the pause is released on command
    /// rather than by the clock.
    /// </param>
    public PausedSearch(TimeSpan pause, Func<TimeSpan, CancellationToken, Task>? wait = null)
    {
        _pause = pause;
        _wait = wait ?? ((delay, token) => Task.Delay(delay, token));
    }

    /// <summary>
    /// Abandons the pause in flight and starts a new one, then runs <paramref name="read"/> as the newest attempt if
    /// nothing replaced it first.
    /// </summary>
    /// <param name="read">The read to ask for, given its attempt number and a token cancelled when typing resumes.</param>
    /// <returns>The number the read was given.</returns>
    public Task<long> RestartAsync(Func<long, CancellationToken, Task> read) =>
        RunAsync(read, waitsOutThePause: true);

    /// <summary>
    /// Abandons the pause in flight and runs <paramref name="read"/> now, for the one case that must not wait: a
    /// submitted term the requester is done typing.
    /// </summary>
    /// <param name="read">The read to ask for, given its attempt number and a token cancelled when typing resumes.</param>
    /// <returns>The number the read was given.</returns>
    public Task<long> RunNowAsync(Func<long, CancellationToken, Task> read) =>
        RunAsync(read, waitsOutThePause: false);

    /// <summary>Abandons the pause in flight, so a read that has not been asked for yet is never asked for.</summary>
    public void Cancel() => Abandon();

    /// <summary>Abandons whatever is in flight.</summary>
    public void Dispose() => Abandon();

    private async Task<long> RunAsync(Func<long, CancellationToken, Task> read, bool waitsOutThePause)
    {
        ArgumentNullException.ThrowIfNull(read);

        Abandon();

        var source = new CancellationTokenSource();
        _pending = source;
        var attempt = Interlocked.Increment(ref _attempt);
        var token = source.Token;

        try
        {
            if (waitsOutThePause)
            {
                await _wait(_pause, token).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            await read(attempt, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Typing resumed, so this pause's question is no longer the one to ask. A read the plant already answered
            // arrives as a result instead, and the reader discards it by its attempt number.
        }
        finally
        {
            if (ReferenceEquals(_pending, source))
            {
                _pending = null;
            }

            source.Dispose();
        }

        return attempt;
    }

    private void Abandon()
    {
        var pending = _pending;
        _pending = null;
        pending?.Cancel();
    }
}
