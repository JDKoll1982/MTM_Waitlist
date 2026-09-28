using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The external-system priming: it settles whether the read-only external system answers, and it answers for the
/// catalogue's <c>visual-priming</c> entry (`contracts/launch-step-contract.md` §1.1; FR-002, FR-027).
/// </summary>
/// <remarks>
/// <para>
/// <b>It runs before the first screen opens, which is the whole point.</b> FR-027 requires the verdict to be
/// settled before the main screens open, so the catalogue places this operation last before the hand-over and the
/// launch awaits it there. Settling it during the launch is what keeps the hysteresis off the first read of a
/// session: the second probe failure that flips the verdict lands while the window is still showing rather than
/// after the shell is already accepting input.
/// </para>
/// <para>
/// <b>It cannot hold the launch, and that is not a contradiction.</b> The entry is best effort and bounded at
/// five seconds, so the launch waits for the verdict only as long as the bound allows and then carries on. The
/// probe loop keeps running afterwards, so an inconclusive prime costs nothing beyond leaving the verdict where
/// it was, which is exactly the behaviour the application had before priming existed.
/// </para>
/// <para>
/// <b>It asks the owner of the probe rather than probing itself.</b> Only one thing is permitted to touch the
/// external system for reachability, and a second probe path with its own idea of what "unreachable" means would
/// be a second answer to the same question.
/// </para>
/// </remarks>
public sealed class VisualVerdictPrimingStep : ILaunchStep
{
    /// <summary>The catalogue entry this step answers for.</summary>
    private const string StepId = "visual-priming";

    private readonly LaunchStep _descriptor;
    private readonly IVisualVerdictPrimer _verdictPrimer;

    /// <summary>Creates the step over the one thing allowed to probe the external system.</summary>
    /// <param name="catalog">The sequence, which is where the step takes its descriptor from rather than restating it.</param>
    /// <param name="verdictPrimer">The probe owner, asked once to settle the verdict.</param>
    /// <exception cref="ArgumentNullException"><paramref name="verdictPrimer"/> is <c>null</c>.</exception>
    public VisualVerdictPrimingStep(LaunchStepCatalog catalog, IVisualVerdictPrimer verdictPrimer)
    {
        ArgumentNullException.ThrowIfNull(verdictPrimer);

        _descriptor = LaunchStepSupport.DescriptorFor(catalog, StepId);
        _verdictPrimer = verdictPrimer;
    }

    /// <inheritdoc />
    public LaunchStep Descriptor => _descriptor;

    /// <inheritdoc />
    public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        return LaunchStepSupport.RunGuardedAsync(
            "The external system could not be asked whether it answers",
            PrimeAsync,
            cancellationToken);
    }

    /// <summary>Asks the probe owner to settle the verdict, and reports that it was asked.</summary>
    /// <param name="cancellationToken">Cancelled when the launch is abandoned or the stated maximum passes.</param>
    /// <remarks>
    /// An inconclusive prime is a normal outcome rather than a failure, so this reports success either way. What
    /// it must not do is claim a verdict it was never told: the wording says the external system was asked, which
    /// is the one thing this step knows.
    /// </remarks>
    private async Task<LaunchStepOutcome> PrimeAsync(CancellationToken cancellationToken)
    {
        await _verdictPrimer.PrimeAsync(cancellationToken).ConfigureAwait(false);

        return new LaunchStepOutcome(
            LaunchStepStatus.Succeeded,
            "The read-only external system was asked whether it answers, before the first screen opens.",
            LaunchRemedySet.None);
    }
}
