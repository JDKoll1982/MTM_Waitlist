using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The reset policy and the repair: it decides which faults may be put right without asking, and it drives every
/// reset through <see cref="IMachineConfigurationService.ResetToDefaultsAsync"/> so the targeted reset has one
/// implementation (FR-018, FR-019; <c>contracts/machine-configuration-contract.md</c> §4).
/// </summary>
/// <remarks>
/// <para>
/// <b>It owns the policy; the configuration service owns the mechanism.</b> This service never writes a
/// configuration row and never builds a statement: it narrows what it is asked to reset and hands the parts to
/// the one implementation, which is also the only code that writes this computer's rows. A second writer beside
/// that one would be a second answer to what a reset means.
/// </para>
/// <para>
/// <b>A reset reaches only this computer's own configuration.</b> Every part is filtered against
/// <see cref="MachineConfigurationParts"/>' own tokens before anything is handed on, so a caller that named a
/// person, a role or a permission gets a reset that touches nothing rather than one that reaches outside this
/// machine (FR-018). The filter is the service's own promise rather than the caller's good behaviour.
/// </para>
/// <para>
/// <b>A repair made without asking may never touch a choice.</b>
/// <see cref="PartsRepairableWithoutAsking"/> names the parts whose value the scope supplies, so restoring one
/// costs nothing the person decided. A repair that touches the display name, the description or the picture
/// sources is refused here and reaches the screen as a question instead (FR-017, FR-018, FR-019).
/// </para>
/// <para>
/// <b>A repair is attempted once per process.</b> A fault that came back after being put right quietly is a
/// fault the person is asked about, so the second attempt answers that nothing was repaired and the stop reaches
/// the screen. Without that rule a fault that survived its own repair would be repaired again on every stop, and
/// the launch would go round the loop without ever saying anything.
/// </para>
/// <para>
/// <b>A repair that could not be made is not a failure of the launch.</b> A store that refused the write, or one
/// that took longer than the launch's own ceiling, leaves the fault where it was and the person is asked about
/// it. It is recorded as a diagnostic and never raised, because a repair the person never asked for must not be
/// the thing that ends their launch.
/// </para>
/// </remarks>
public sealed class StartupRecoveryService
{
    /// <summary>The parts a repair may touch without asking, in the order the reset restores them.</summary>
    /// <remarks>
    /// A scoped preference row is supplied by the scope it belongs to, so a broken row restored to the value that
    /// scope resolves is this computer returned to the state it would have been in without the fault. Nothing the
    /// person chose is anywhere in that sentence, which is why no question is asked (FR-019).
    /// </remarks>
    private static readonly string[] s_partsRepairableWithoutAsking = [MachineConfigurationParts.ScopedPreference];

    /// <summary>
    /// Every part a reset may reach, which is the whole of this computer's own configuration and nothing else.
    /// </summary>
    private static readonly string[] s_thisMachinesParts =
    [
        MachineConfigurationParts.Configuration,
        MachineConfigurationParts.DisplayName,
        MachineConfigurationParts.Description,
        MachineConfigurationParts.ScopedPreference,
    ];

    /// <summary>What a repair answers when it changed nothing, which is an answer rather than a failure.</summary>
    private static readonly MachineConfigurationResetResult s_nothingRepaired = new(
        Succeeded: true,
        Reset: [],
        FailureReason: null);

    private readonly IMachineConfigurationService _configuration;

    /// <summary>Set the first time a repair is attempted, so a fault that survived one is asked about instead.</summary>
    private int _repairAttempted;

    /// <summary>Creates the policy over the one implementation of a targeted reset.</summary>
    /// <param name="configuration">The only code that writes this computer's configuration rows.</param>
    /// <exception cref="ArgumentNullException">The configuration service is <c>null</c>.</exception>
    public StartupRecoveryService(IMachineConfigurationService configuration)
        => _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    /// <summary>The parts a repair may put right without asking, which the remedy table's own list must match.</summary>
    internal static IReadOnlyList<string> PartsRepairableWithoutAsking => s_partsRepairableWithoutAsking;

    /// <summary>
    /// Restores the parts the person agreed to, and only this computer's own configuration among them (FR-018).
    /// </summary>
    /// <param name="whatIsBroken">The parts a reset was agreed for, as <see cref="MachineConfigurationParts"/> tokens.</param>
    /// <param name="cancellationToken">Cancels the store write.</param>
    /// <returns>The outcome, naming the parts that were reset.</returns>
    /// <remarks>
    /// A token that names nothing of this computer's is dropped before the reset runs, so the parts the person
    /// agreed to and the parts that are restored are the same set. A drop can only mean the caller named
    /// something a reset may not reach, which is the case this method exists to make impossible. A request that
    /// names nothing of this computer's at all is answered without going near the store, because there would be
    /// nothing for the store to do.
    /// </remarks>
    public Task<MachineConfigurationResetResult> RestoreDefaultsAsync(
        IReadOnlyList<string> whatIsBroken,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(whatIsBroken);

        var parts = Narrow(whatIsBroken, s_thisMachinesParts);

        return parts.Count == 0
            ? Task.FromResult(s_nothingRepaired)
            : _configuration.ResetToDefaultsAsync(parts, cancellationToken);
    }

    /// <summary>
    /// Puts a fault right without asking the person, and answers what it changed (FR-019).
    /// </summary>
    /// <param name="whatIsBroken">The parts a repair may touch, as <see cref="MachineConfigurationParts"/> tokens.</param>
    /// <param name="cancellationToken">Cancels the store write, which is the caller abandoning the launch.</param>
    /// <returns>
    /// The outcome. An empty <see cref="MachineConfigurationResetResult.Reset"/> means nothing was put right, so
    /// the fault stands and the person is asked about it rather than the repair being tried again.
    /// </returns>
    /// <remarks>
    /// The attempt is bounded by the launch's own ceiling, because a repair the person never asked for must not be
    /// the reason their launch window sits still. A bound reached, a store that refused the write and a part this
    /// service may not touch without asking all answer the same way: nothing was repaired.
    /// </remarks>
    public async Task<MachineConfigurationResetResult> RepairWithoutAskingAsync(
        IReadOnlyList<string> whatIsBroken,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(whatIsBroken);

        var parts = Narrow(whatIsBroken, s_partsRepairableWithoutAsking);

        // Nothing to repair quietly, or a repair has already been tried and the fault survived it, which is the
        // one case where the person is the answer rather than a second attempt (FR-019).
        if (parts.Count == 0 || Interlocked.Exchange(ref _repairAttempted, 1) == 1)
        {
            return s_nothingRepaired;
        }

        using var bound = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        bound.CancelAfter(LaunchStepCatalog.Ceiling);

        try
        {
            return await _configuration.ResetToDefaultsAsync(parts, bound.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // The fault is left where it was and the person is asked about it. Nothing is raised, because a repair
            // they never asked for must not be what ends their launch.
            AppLog.Error(
                "StartupRecovery",
                exception,
                "A fault that could be repaired without asking was not repaired, so the person is being asked about it.");

            return s_nothingRepaired;
        }
    }

    /// <summary>
    /// Narrows a request to the parts it is allowed to reach, so a reset can never be aimed at a person, a role or
    /// a permission (FR-018).
    /// </summary>
    /// <param name="whatIsBroken">What a caller asked to reset.</param>
    /// <param name="allowed">The parts this request may reach.</param>
    /// <returns>The tokens among them that were asked for, in the order they were asked for.</returns>
    /// <remarks>
    /// One filter serves both the reset a person agreed to and the repair made without asking, so the rule about
    /// what may be reached has one statement rather than one per caller. A token that names nothing known is
    /// dropped rather than trimmed into something else: a reset that guessed at what it was asked for would be a
    /// reset nobody agreed to.
    /// </remarks>
    private static List<string> Narrow(IReadOnlyList<string> whatIsBroken, string[] allowed)
        => whatIsBroken
            .Select(part => part?.Trim() ?? string.Empty)
            .Where(part => allowed.Contains(part, StringComparer.Ordinal))
            .ToList();
}
