using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The step the runner drives (`contracts/launch-step-contract.md` §2).
/// </summary>
/// <remarks>
/// <para>
/// <b>A step describes itself and does one piece of work.</b> The descriptor is data the catalog declares; the
/// step is the code that performs it. The runner announces the descriptor's name before calling
/// <see cref="RunAsync"/>, so a step does not announce itself and cannot forget to.
/// </para>
/// <para>
/// <b>A step reports, it does not route.</b> It returns an outcome describing what happened; deciding what the
/// launch does about it belongs to the pipeline (FR-001).
/// </para>
/// </remarks>
public interface ILaunchStep
{
    /// <summary>The step's data: its id, its name, its category and its stated maximum.</summary>
    LaunchStep Descriptor { get; }

    /// <summary>
    /// Performs the step's work.
    /// </summary>
    /// <param name="context">The person (once resolved), the machine's facts and the feed.</param>
    /// <param name="cancellationToken">
    /// Cancelled when the launch is abandoned, and also when the step passes its stated maximum — so a step that
    /// honours the token is stopped at its bound rather than left running.
    /// </param>
    /// <returns>What the step did, in plain language, and what the person may do about it if it failed.</returns>
    Task<LaunchStepOutcome> RunAsync(
        LaunchStepContext context,
        CancellationToken cancellationToken);
}
