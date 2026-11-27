using Microsoft.Extensions.Options;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The launch's second step: it confirms the store that holds the records answers, and it answers for the
/// catalogue's <c>store-reachability</c> entry (`contracts/launch-step-contract.md` §1.1; FR-002, FR-003).
/// </summary>
/// <remarks>
/// <para>
/// <b>It proves a round trip, not a row.</b> The step calls a stored procedure the store already publishes and
/// discards what comes back, so the verdict is "the store answered" rather than a statement about any record. It
/// reads the registry the machine check is about to consult, so the round trip is against the store the launch
/// actually depends on.
/// </para>
/// <para>
/// <b>It calls a service, and the service calls a stored procedure.</b> No statement text lives here, and the
/// store is never read directly (constitution III; `contracts/launch-step-contract.md` §2).
/// </para>
/// <para>
/// <b>A store that does not answer is a stop with a retry and no reset.</b> Resetting this machine's
/// configuration could not remove an outage, so the remedy offered is the repeat (FR-016, FR-017), and the
/// diagnosis names the store and carries what the store seam reported (FR-004).
/// </para>
/// </remarks>
public sealed class StoreReachabilityStep : ILaunchStep
{
    /// <summary>The catalogue entry this step answers for.</summary>
    private const string StepId = "store-reachability";

    /// <summary>
    /// The stored procedure the probe calls: a read the store already publishes, over the registry the machine
    /// check consults, so nothing new is added to the store for a reachability check.
    /// </summary>
    private const string ReachabilityRead = "sp_core_computers_registry_get_all";

    /// <summary>The probe's parameters, which are none: the read takes no argument.</summary>
    private static readonly IReadOnlyDictionary<string, object?> s_noParameters = new Dictionary<string, object?>();

    private readonly LaunchStep _descriptor;
    private readonly IMySqlHelperServer _store;
    private readonly WaitlistDatabaseOptions _waitlistSettings;
    private readonly IStoreAvailabilityTracker? _availability;

    /// <summary>Creates the step over the store seam every internal read and write goes to.</summary>
    /// <param name="catalog">The sequence, which is where the step takes its descriptor from rather than restating it.</param>
    /// <param name="store">The data-access seam, which turns the call into a stored-procedure call.</param>
    /// <param name="waitlistSettings">
    /// This computer's settings for the records store. The seam returns a neutral result for a store it has no
    /// connection for, so without this the step would report "the store answered" on nothing (FR-019).
    /// </param>
    /// <param name="availability">
    /// What the seam recorded about the last read. A store that is configured but silent answers the seam with the
    /// same neutral result an empty store would, so the step would otherwise report success on an outage. Optional
    /// so a test can state the host it describes rather than depending on a tracker being wired.
    /// </param>
    public StoreReachabilityStep(
        LaunchStepCatalog catalog,
        IMySqlHelperServer store,
        IOptions<WaitlistDatabaseOptions> waitlistSettings,
        IStoreAvailabilityTracker? availability = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(waitlistSettings);

        _descriptor = LaunchStepSupport.DescriptorFor(catalog, StepId);
        _store = store;
        _waitlistSettings = waitlistSettings.Value;
        _availability = availability;
    }

    /// <inheritdoc />
    public LaunchStep Descriptor => _descriptor;

    /// <inheritdoc />
    public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        // The seam answers a store it has no connection for with a neutral result, which is right for a screen
        // that should not show an outage it cannot fix. It is wrong for this step, whose whole answer is "the
        // store answered": a connection nothing supplies is refused here, naming the variable (FR-019).
        var requirement = ConfigurationRequirement.WaitlistStore(_waitlistSettings);
        if (!requirement.IsSatisfied)
        {
            return Task.FromResult(new LaunchStepOutcome(
                LaunchStepStatus.Failed,
                requirement.RefusalMessage,
                LaunchRemedySet.RetryOnly));
        }

        return LaunchStepSupport.RunGuardedAsync(
            "The store did not answer",
            async token =>
            {
                await _store
                    .ExecuteStoredProcedureQueryAsync(
                        ReachabilityRead,
                        s_noParameters,
                        MySqlDatabaseTarget.MtmWaitlist,
                        token)
                    .ConfigureAwait(false);

                // The seam answers a store that did not answer with the same neutral result an empty read would
                // produce, so the read returning is not evidence that the store answered. What the seam recorded
                // is: a configured store that was silent is an outage, and the launch stops here rather than
                // opening a shell that can do nothing (FR-019, FR-021).
                var observed = _availability?.Get(MySqlDatabaseTarget.MtmWaitlist);
                if (observed is { Status: InternalStoreStatus.Unavailable })
                {
                    return new LaunchStepOutcome(
                        LaunchStepStatus.Failed,
                        string.IsNullOrWhiteSpace(observed.Message)
                            ? "The store did not answer."
                            : observed.Message,
                        LaunchRemedySet.RetryOnly);
                }

                return new LaunchStepOutcome(
                    LaunchStepStatus.Succeeded,
                    "The store answered.",
                    LaunchRemedySet.None);
            },
            cancellationToken);
    }
}
