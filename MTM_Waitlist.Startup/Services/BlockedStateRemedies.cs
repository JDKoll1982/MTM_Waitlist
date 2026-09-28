using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// What stopped the launch, as the coarse cause a remedy is matched to (FR-004, FR-017).
/// </summary>
/// <remarks>
/// <para>
/// <b>Two causes, because the data model's remedy table has one row for each.</b> A cause is the answer to
/// "could anything this screen offers remove it", so two stops whose remedies are identical are one cause here
/// however differently they read. The words a person sees are the failing line's own diagnosis and never this
/// value, so nothing is lost by keeping the set small.
/// </para>
/// <para>
/// <b>A store that answered slowly is the store cause.</b> The store that passed a step's stated maximum and
/// the store that refused the connection are one row in the table, and neither offers a reset: no reset can
/// make a store answer, and offering one would invite an answer that was never the problem (FR-003, FR-017).
/// </para>
/// </remarks>
public enum BlockedStateCause
{
    /// <summary>
    /// The store could not be reached, answered too slowly, or answered for a part of the launch whose rows
    /// live there. It is the answer for every stop that is not about this computer's own rows, which is the safe
    /// one: it offers the least, so a fault that is really the store's can never be answered with a reset.
    /// </summary>
    StoreUnreachable,

    /// <summary>
    /// This computer's own configuration could not be read or written: its identity, its description or the
    /// places its pictures come from. A reset of exactly those rows could remove the cause, so the restore is
    /// offered (FR-017, FR-018).
    /// </summary>
    MachineConfigurationBroken,
}

/// <summary>
/// The cause-to-remedy table: what stopped the launch, what may be done about it, and exactly what a reset would
/// touch (`contracts/launch-step-contract.md` §4, `contracts/machine-configuration-contract.md` §4; FR-016,
/// FR-017, FR-018, FR-019).
/// </summary>
/// <remarks>
/// <para>
/// <b>A remedy is offered only where it could remove the cause.</b> A store that cannot be reached never offers a
/// reset (FR-017), because restoring this computer's own rows cannot make a store answer. This table is the one
/// place that decision is made, so the screen never has to judge it and no second copy of the rule can drift.
/// </para>
/// <para>
/// <b>The preview and the reset describe the same set.</b> <see cref="ResetPartsFor"/> names the parts a reset
/// would touch and <see cref="For"/> composes its sentence from those very parts, so a part added to the reset
/// cannot leave the sentence promising something the reset does not do (FR-018).
/// </para>
/// <para>
/// <b>Some parts are put right without asking.</b> <see cref="SilentRepairPartsFor"/> names the parts whose value
/// comes from the scope that supplies it rather than from anything the person typed on this screen. Restoring one
/// of those costs nothing they chose, so the fault is repaired rather than made a question (FR-019). Everything
/// the person did choose is asked about, which is why the display name and the description are never repaired
/// without them.
/// </para>
/// <para>
/// <b>Nothing here touches a person, a role or a permission.</b> Every token this table can produce is one of
/// <see cref="MachineConfigurationParts"/>' own, which name rows belonging to this computer and to nothing else
/// (FR-018).
/// </para>
/// </remarks>
public static class BlockedStateRemedies
{
    /// <summary>
    /// The step whose failure is about this computer's own rows rather than about a store-wide read: the save
    /// that names this computer and points it at its shared pictures.
    /// </summary>
    private const string SaveConfigurationStepId = "save-machine-configuration";

    /// <summary>
    /// The step that reads this computer's own configuration, which is the other half of the same cause: a read
    /// that could not be made leaves the same rows the reset restores.
    /// </summary>
    private const string ReadConfigurationStepId = "read-machine-configuration";

    /// <summary>
    /// The parts a repair may touch without asking, in the order the reset reports them.
    /// </summary>
    /// <remarks>
    /// A scoped preference row that cannot be read is reset to the value the scope resolves without it, which is
    /// the value this computer would have had if the broken row had never been written. Nothing the person chose
    /// is lost, so there is nothing to ask them about (FR-019).
    /// </remarks>
    private static readonly string[] s_partsRepairedWithoutAsking = [MachineConfigurationParts.ScopedPreference];

    /// <summary>
    /// The parts a reset of this computer's configuration restores, which is what the person is asked about.
    /// </summary>
    /// <remarks>
    /// The display name, the description and the picture sources are all things a person typed or chose for this
    /// computer, so a reset that touches one of them is a question rather than a courtesy (FR-018).
    /// </remarks>
    private static readonly string[] s_configurationParts =
    [
        MachineConfigurationParts.DisplayName,
        MachineConfigurationParts.Description,
        MachineConfigurationParts.PictureSources,
    ];

    /// <summary>
    /// Which cause a failed step represents, so a stop can be matched to a remedy without guessing (FR-004).
    /// </summary>
    /// <param name="failedStep">The step that stopped the launch, or <c>null</c> when the feed names no failure.</param>
    /// <returns>The cause the step's failure represents.</returns>
    /// <remarks>
    /// It is read from the step rather than from the prose of its failure, because the prose is written for a
    /// person to read and is free to change. A step this computer cannot run without its own rows is the one
    /// cause a reset can help; everything else is the store's, which is also the answer when nothing was named,
    /// because that is the cause offering the least.
    /// </remarks>
    public static BlockedStateCause Classify(LaunchStep? failedStep)
    {
        if (failedStep is null)
        {
            return BlockedStateCause.StoreUnreachable;
        }

        return string.Equals(failedStep.Id, SaveConfigurationStepId, StringComparison.Ordinal)
            || string.Equals(failedStep.Id, ReadConfigurationStepId, StringComparison.Ordinal)
            ? BlockedStateCause.MachineConfigurationBroken
            : BlockedStateCause.StoreUnreachable;
    }

    /// <summary>
    /// The actions a stop of this cause may offer, and what a reset would touch when one is offered (FR-016,
    /// FR-017, FR-018).
    /// </summary>
    /// <param name="cause">What stopped the launch.</param>
    /// <returns>
    /// The remedy set the surface draws. Wherever a reset is offered its preview names the parts the reset would
    /// restore, composed from those parts themselves so the two cannot disagree.
    /// </returns>
    public static LaunchRemedySet For(BlockedStateCause cause)
    {
        var resetParts = ResetPartsFor(cause);

        // The repeat is offered for every stop: every failed step is repeatable, and repeating it is the one
        // action that costs nothing when it does not help (FR-016).
        return resetParts.Count == 0
            ? LaunchRemedySet.RetryOnly
            : new LaunchRemedySet(
                CanRetry: true,
                CanRestoreDefaults: true,
                RestoreDefaultsPreview: ComposePreview(resetParts));
    }

    /// <summary>The parts a reset of this cause would restore, or an empty list when no reset is offered.</summary>
    /// <param name="cause">What stopped the launch.</param>
    /// <returns>The part tokens, in the order the reset restores them.</returns>
    public static IReadOnlyList<string> ResetPartsFor(BlockedStateCause cause)
        => cause is BlockedStateCause.MachineConfigurationBroken ? s_configurationParts : [];

    /// <summary>
    /// The parts a repair may put right without asking about this cause, or an empty list when the person has to
    /// be asked (FR-019).
    /// </summary>
    /// <param name="cause">What stopped the launch.</param>
    /// <returns>The part tokens a repair may touch without asking.</returns>
    public static IReadOnlyList<string> SilentRepairPartsFor(BlockedStateCause cause)
        => cause is BlockedStateCause.MachineConfigurationBroken ? s_partsRepairedWithoutAsking : [];

    /// <summary>
    /// One part of this computer's configuration as a person reads it, for the sentence a reset preview shows.
    /// </summary>
    /// <param name="part">One of <see cref="MachineConfigurationParts"/>' tokens.</param>
    /// <returns>The part's name in the reader's own words, or the token itself when it names nothing known.</returns>
    /// <remarks>
    /// An unknown token is said as itself rather than dropped: a preview that silently left something out would be
    /// a preview that did not name what the reset would touch (FR-018).
    /// </remarks>
    internal static string DescribePart(string part) => part?.Trim() switch
    {
        MachineConfigurationParts.DisplayName => "Startup_BlockedState.PartDisplayName".GetLocalized(),
        MachineConfigurationParts.Description => "Startup_BlockedState.PartDescription".GetLocalized(),
        MachineConfigurationParts.PictureSources => "Startup_BlockedState.PartPictureSources".GetLocalized(),
        MachineConfigurationParts.ScopedPreference => "Startup_BlockedState.PartScopedPreference".GetLocalized(),
        null or "" => string.Empty,
        _ => part.Trim(),
    };

    /// <summary>
    /// The sentence a reset preview shows, named one part per line so the list is read rather than skimmed.
    /// </summary>
    /// <param name="parts">The parts the reset would restore.</param>
    /// <returns>The preview, one part to a line.</returns>
    private static string ComposePreview(IReadOnlyList<string> parts)
        => string.Join(
            Environment.NewLine,
            parts.Select(part => $"\u2022 {DescribePart(part)}"));
}
