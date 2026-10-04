using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The launch, behind one entry point (`contracts/launch-step-contract.md` §5; FR-001, FR-006, FR-008, FR-020).
/// </summary>
/// <remarks>
/// <para>
/// <b>The host owns one call and two events.</b> It calls <see cref="RunAsync"/> once as the application starts,
/// and it listens for <see cref="ShellReady"/> to be handed a surface and for <see cref="ProcessEnding"/> to
/// state a reason before the process goes. It does not own window handoff: which surface to show is decided by
/// the launch, and the host only shows it (S4).
/// </para>
/// <para>
/// <b>The pipeline refuses to continue while the machine is unconfigured.</b> That refusal is here rather than
/// on the setup screen, so a dismissal route the screen never sees still cannot reach the main screens
/// (FR-006, S10.1).
/// </para>
/// </remarks>
public interface ILaunchPipeline
{
    /// <summary>
    /// Runs the launch from its first step and answers where it ended.
    /// </summary>
    /// <param name="cancellationToken">Cancels the launch, which ends it without a step being reported failed.</param>
    /// <returns>
    /// Exactly one of the five outcomes (FR-001). A step that failed answers
    /// <see cref="LaunchOutcome.Blocked"/>, because the work that stopped the launch is repeatable (FR-016).
    /// </returns>
    /// <remarks>
    /// A second entry while a sequence is already running starts nothing and answers the run in progress, so a
    /// re-entrant navigation cannot run two pipelines against one machine.
    /// </remarks>
    Task<LaunchOutcome> RunAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Repeats the named step and the steps after it, and nothing before it.
    /// </summary>
    /// <param name="failedStepId">The step to resume at, which must be one the launch declares.</param>
    /// <param name="cancellationToken">Cancels the launch.</param>
    /// <returns>The outcome the repeated sequence ended at.</returns>
    /// <remarks>
    /// This is FR-020: a store-only retry never re-runs step 1, and a retry after machine setup resumes at the
    /// save rather than at the beginning.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The launch declares no step with that id, so there is nothing to resume at.
    /// </exception>
    Task<LaunchOutcome> RetryFromAsync(string failedStepId, CancellationToken cancellationToken);

    /// <summary>
    /// States why the process is ending and answers <see cref="LaunchOutcome.Ended"/> (FR-008).
    /// </summary>
    /// <param name="reason">Why the process is going, in plain language, stated before it goes.</param>
    /// <returns><see cref="LaunchOutcome.Ended"/>.</returns>
    /// <remarks>
    /// Every abort route reaches the host through this: an abandoned machine setup, a blocked launch the person
    /// closed. It is the only way to reach <see cref="ProcessEnding"/>, so an abort is stated rather than
    /// reported as a crash.
    /// </remarks>
    LaunchOutcome End(string reason);

    /// <summary>
    /// Raised when the launch has a surface to hand the person to, carrying where it ended.
    /// </summary>
    /// <remarks>
    /// The shell is reachable only through this event (S4): the host has no other way to learn that the launch
    /// succeeded, and no continue-anyway path exists beside it.
    /// </remarks>
    event EventHandler<LaunchOutcome>? ShellReady;

    /// <summary>
    /// Raised when the process is about to end, carrying the reason to state before it goes (FR-008).
    /// </summary>
    event EventHandler<string>? ProcessEnding;
}
