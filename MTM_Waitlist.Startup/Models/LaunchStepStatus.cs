namespace MTM_Waitlist.Module_Startup.Models;

/// <summary>
/// How one launch step ended (`contracts/launch-step-contract.md` §2).
/// </summary>
/// <remarks>
/// The three values answer three different questions, so they are not a severity scale: <see cref="Succeeded"/>
/// means the work was done, <see cref="Skipped"/> means the work was left undone and the launch carries on, and
/// <see cref="Failed"/> means the work did not finish. A best-effort step that fails is reported as
/// <see cref="Skipped"/> to its caller — it is recorded as a failure on the feed, but it cannot stop the launch
/// (FR-026).
/// </remarks>
public enum LaunchStepStatus
{
    /// <summary>The step's work finished and the launch carries on.</summary>
    Succeeded,

    /// <summary>
    /// The step did not do its work and the launch carries on anyway. A best-effort step reports this for any
    /// failure, and a step may also report it because its work did not apply — there was nothing to do.
    /// </summary>
    Skipped,

    /// <summary>The step's work did not finish, and the launch may not carry on past it.</summary>
    Failed,
}
