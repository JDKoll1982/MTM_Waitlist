using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// This machine's own configuration: whether it is configured, and — when it is not — why not
/// (FR-006, FR-009, FR-018; <c>contracts/machine-configuration-contract.md</c> sections 1 and 4).
/// </summary>
/// <remarks>
/// <para>
/// <b>One writer for these rows.</b> <c>MachineConfigurationService</c> is the only code that writes this
/// machine's display name, its description and its picture sources. The launch pipeline's setup step saves
/// through <see cref="SaveAsync"/>, and the recovery step drives a reset through
/// <see cref="ResetToDefaultsAsync"/> rather than writing configuration rows itself, so a targeted reset has one
/// implementation rather than two.
/// </para>
/// <para>
/// <b>Four ways to be unconfigured, told apart.</b> A machine that has never been configured, one whose
/// configuration was removed, one whose configuration was revoked and one whose configuration cannot be read all
/// resolve to <c>IsConfigured == false</c>, but they do not resolve to the same reason: the screen has to say
/// which of them happened (FR-004), and only a specific reason lets a remedy be offered that could work (FR-017).
/// </para>
/// <para>
/// <b>Records are not part of it.</b> Records live only in the store (FR-025), so machine configuration is the
/// machine's identity label and where its pictures come from, and nothing else.
/// </para>
/// </remarks>
public interface IMachineConfigurationService
{
    /// <summary>
    /// Reads this machine's configuration and answers whether it is configured, carrying the reason when it is
    /// not.
    /// </summary>
    /// <param name="cancellationToken">Cancels the store reads.</param>
    /// <returns>
    /// The state. An unreadable store is reported as the unconfigured reason rather than raised, because losing
    /// configuration never degrades into running without it (FR-009).
    /// </returns>
    Task<MachineConfigurationState> GetStateAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Writes this machine's configuration: its display name and description, and its three picture sources.
    /// </summary>
    /// <param name="draft">What the setup screen captured.</param>
    /// <param name="cancellationToken">Cancels the store writes.</param>
    /// <returns>
    /// The outcome. A display name another machine already holds is refused here and the screen asks for a
    /// different one, rather than the write being attempted and colliding on the store's unique key.
    /// </returns>
    Task<MachineConfigurationSaveResult> SaveAsync(
        MachineConfigurationDraft draft,
        CancellationToken cancellationToken);

    /// <summary>
    /// Restores this machine's configuration to its defaults, and only the parts named as broken.
    /// </summary>
    /// <param name="whatIsBroken">
    /// The parts to reset, as <see cref="MachineConfigurationParts"/> tokens. A part that is not named is not
    /// touched, so the preview the screen shows and the reset that runs describe the same set.
    /// </param>
    /// <param name="cancellationToken">Cancels the store writes.</param>
    /// <returns>The outcome, naming the parts that were reset.</returns>
    Task<MachineConfigurationResetResult> ResetToDefaultsAsync(
        IReadOnlyList<string> whatIsBroken,
        CancellationToken cancellationToken);
}
