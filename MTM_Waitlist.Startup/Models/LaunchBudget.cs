using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Models;

/// <summary>
/// The launch-wide budget: one ceiling of about 45 seconds for a whole launch to reach either the shell or a
/// stated stop (`contracts/launch-concurrency-contract.md` §2; FR-024, FR-025).
/// </summary>
/// <remarks>
/// <para>
/// <b>It is additional to each step's stated maximum, never a replacement.</b> Every step still runs under its
/// own <see cref="LaunchStep.MaximumWait"/> through the runner; this budget only puts a ceiling over the walk as
/// a whole, so a launch made of many bounded steps cannot add up to an unbounded wait (FR-025, FR-036).
/// </para>
/// <para>
/// <b>Passing it ends the launch at a stated stop that names the budget.</b> <see cref="IsReached"/> is true only
/// when the budget's own clock fired; an outer cancellation is the person abandoning the launch, which is not a
/// budget overrun and must not be reported as one (FR-024).
/// </para>
/// </remarks>
public sealed class LaunchBudget : IDisposable
{
    /// <summary>The launch-wide ceiling: approximately 45 seconds (FR-024).</summary>
    public static TimeSpan DefaultDuration { get; } = TimeSpan.FromSeconds(45);

    private readonly CancellationTokenSource _holder;
    private readonly CancellationToken _outer;

    private bool _completed;

    /// <summary>
    /// Creates the budget for one launch, linked to whatever token the caller already holds.
    /// </summary>
    /// <param name="outerToken">The token that cancels when the launch is abandoned altogether.</param>
    /// <param name="duration">The ceiling, defaulting to <see cref="DefaultDuration"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">A duration that is not positive.</exception>
    public LaunchBudget(CancellationToken outerToken, TimeSpan? duration = null)
    {
        Duration = duration ?? DefaultDuration;

        if (Duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(duration),
                Duration,
                "A launch budget of no time is not a budget: the launch would stop before it started (FR-024).");
        }

        _outer = outerToken;
        _holder = CancellationTokenSource.CreateLinkedTokenSource(outerToken);
        _holder.CancelAfter(Duration);
    }

    /// <summary>The ceiling this launch is held to.</summary>
    public TimeSpan Duration { get; }

    /// <summary>Whether the walk reached the shell or a stated stop before the budget passed.</summary>
    public bool IsCompleted => _completed;

    /// <summary>
    /// Whether the launch passed its budget, which is a stated stop whose cause names the budget (FR-024).
    /// </summary>
    /// <remarks>
    /// An outer cancellation is the launch being abandoned, not the budget being passed, so it answers false.
    /// </remarks>
    public bool IsReached => !_completed && _holder.IsCancellationRequested && !_outer.IsCancellationRequested;

    /// <summary>The token every operation in the launch is linked to, so the budget bounds the whole walk.</summary>
    public CancellationToken Token => _holder.Token;

    /// <summary>The stop the budget states when it is passed, naming the budget rather than a step (FR-024).</summary>
    public LaunchStepOutcome Overrun => new(
        LaunchStepStatus.Failed,
        $"The launch did not reach a first screen within the {Duration.TotalSeconds:0}-second budget it is allowed.",
        LaunchRemedySet.RetryOnly);

    /// <summary>
    /// Records that the walk ended at an outcome of its own before the budget passed, so a launch that finished in
    /// time is never reported as an overrun.
    /// </summary>
    public void MarkCompleted() => _completed = true;

    /// <inheritdoc />
    public void Dispose() => _holder.Dispose();
}
