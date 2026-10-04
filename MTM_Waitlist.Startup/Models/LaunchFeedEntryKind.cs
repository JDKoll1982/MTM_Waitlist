namespace MTM_Waitlist.Module_Startup.Models;

/// <summary>
/// What one line of the activity feed is about (`contracts/launch-step-contract.md` §3, FR-002).
/// </summary>
/// <remarks>
/// The first three kinds are a step's own transitions, and the fourth is the work inside a step. A step
/// announces itself with <see cref="StepStarted"/> before it runs, so a stall has a name attached to it; the
/// remaining two record how it ended. <see cref="SubOperation"/> names an operation, its target and its outcome,
/// which is what makes a slow store distinguishable from a stalled share.
/// </remarks>
public enum LaunchFeedEntryKind
{
    /// <summary>The step is named before its work begins (FR-002).</summary>
    StepStarted,

    /// <summary>The step ended. <c>Succeeded</c> on the entry says whether it appeared to do its work.</summary>
    StepCompleted,

    /// <summary>The step failed, including a best-effort step whose failure is recorded but never blocking.</summary>
    StepFailed,

    /// <summary>Something a step did, naming the operation, its target and its outcome.</summary>
    SubOperation,
}
