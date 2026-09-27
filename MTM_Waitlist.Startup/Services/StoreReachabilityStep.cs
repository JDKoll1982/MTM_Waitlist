using MTM_Waitlist.Module_Core.Contracts.Services;
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

    /// <summary>Creates the step over the store seam every internal read and write goes to.</summary>
    /// <param name="catalog">The sequence, which is where the step takes its descriptor from rather than restating it.</param>
    /// <param name="store">The data-access seam, which turns the call into a stored-procedure call.</param>
    public StoreReachabilityStep(LaunchStepCatalog catalog, IMySqlHelperServer store)
    {
        ArgumentNullException.ThrowIfNull(store);

        _descriptor = LaunchStepSupport.DescriptorFor(catalog, StepId);
        _store = store;
    }

    /// <inheritdoc />
    public LaunchStep Descriptor => _descriptor;

    /// <inheritdoc />
    public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

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

                return new LaunchStepOutcome(
                    LaunchStepStatus.Succeeded,
                    "The store answered.",
                    LaunchRemedySet.None);
            },
            cancellationToken);
    }
}
