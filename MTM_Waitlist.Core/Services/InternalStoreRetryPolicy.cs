namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// The bounded retry policy every internal-store read follows before a screen reports
/// <c>Unavailable</c> (FR-021, `data-model.md` §10): up to three attempts, with delays of approximately
/// 1 s, 2 s, and 4 s.
/// </summary>
/// <remarks>
/// <para>
/// <b>Bounded is the point.</b> A transient blip is absorbed without the operator seeing anything; a store
/// that is really down produces three attempts and then a single, honest per-screen state — not an unbounded
/// retry loop, not a silent empty list, and not sample data (FR-001).
/// </para>
/// <para>
/// <b>How the three listed delays map onto three attempts.</b> FR-021 gives one sentence — "up to three
/// attempts with delays of approximately 1 s, 2 s, and 4 s" — and `data-model.md` §10 fixes the ending: "after
/// the third failure the screen shows the <c>Unavailable</c> state". So three attempts is the budget, and the
/// schedule is read as: 1 s before the second attempt, 2 s before the third, and the 4 s figure is the
/// <i>next-retry guidance</i> the state advertises after the third failure (it is what
/// <see cref="NextRetryDelay"/> returns and what a screen shows as "next attempt after"). Reading it as three
/// retries after the first attempt would give four attempts, which contradicts "after the third failure".
/// </para>
/// <para>
/// The delay is injectable so tests can assert the attempt count and the reported retry count without waiting
/// seven seconds per case. The delays themselves are part of the contract and are asserted by
/// <c>InternalStoreAvailabilityTests</c>.
/// </para>
/// </remarks>
public sealed class InternalStoreRetryPolicy
{
    /// <summary>Total attempts allowed, including the first one (FR-021: "up to three attempts").</summary>
    public const int DefaultAttemptLimit = 3;

    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    /// <summary>Delays between attempts, plus the next-retry guidance — approximately 1 s, 2 s, and 4 s.</summary>
    public static readonly IReadOnlyList<TimeSpan> DefaultDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
    ];

    /// <summary>The policy the application uses.</summary>
    public static InternalStoreRetryPolicy Default { get; } = new();

    /// <summary>Creates a policy.</summary>
    /// <param name="delays">
    /// Delays before each retry, then the next-retry guidance. Defaults to <see cref="DefaultDelays"/>.
    /// </param>
    /// <param name="delay">Optional delay implementation, so a test can run the policy without waiting.</param>
    /// <param name="maxAttempts">
    /// Total attempts allowed, including the first. Defaults to <see cref="DefaultAttemptLimit"/>; pass 1 for a
    /// single-attempt policy.
    /// </param>
    public InternalStoreRetryPolicy(
        IReadOnlyList<TimeSpan>? delays = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null,
        int maxAttempts = DefaultAttemptLimit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        Delays = delays ?? DefaultDelays;
        MaxAttempts = maxAttempts;
        _delay = delay ?? ((duration, token) => Task.Delay(duration, token));
    }

    /// <summary>The delays between attempts, followed by the next-retry guidance, in order.</summary>
    public IReadOnlyList<TimeSpan> Delays { get; }

    /// <summary>Total attempts allowed, including the first one.</summary>
    public int MaxAttempts { get; }

    /// <summary>
    /// The delay to advertise as "when the next attempt is worth making" after the final attempt failed
    /// (the last entry of <see cref="Delays"/>). Zero when no delay is configured.
    /// </summary>
    public TimeSpan NextRetryDelay => Delays.Count == 0 ? TimeSpan.Zero : Delays[^1];

    /// <summary>Waits before retrying after the given attempt number (1-based).</summary>
    /// <param name="attemptNumber">The attempt that just failed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task DelayBeforeRetryAsync(int attemptNumber, CancellationToken cancellationToken)
    {
        if (Delays.Count == 0 || attemptNumber >= Delays.Count + 1)
        {
            return Task.CompletedTask;
        }

        var index = Math.Clamp(attemptNumber - 1, 0, Delays.Count - 1);
        return _delay(Delays[index], cancellationToken);
    }
}
