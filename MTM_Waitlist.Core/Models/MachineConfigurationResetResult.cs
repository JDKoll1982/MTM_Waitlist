namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// The outcome of restoring this machine's configuration to its defaults (FR-018; contract section 4).
/// </summary>
/// <param name="Succeeded">
/// True when the reset ran. False only when it was refused before writing anything, which is reported through
/// <paramref name="FailureReason"/>.
/// </param>
/// <param name="Reset">
/// The parts of this machine's configuration that were restored, named with
/// <see cref="MachineConfigurationParts"/>. A part the caller did not name is absent, and so is a part this
/// machine had nothing to restore, so the list is what actually happened rather than what was asked for.
/// </param>
/// <param name="FailureReason">
/// One of <see cref="MachineConfigurationRefusals"/>, or <c>null</c> when the reset succeeded.
/// </param>
/// <remarks>
/// As with the save, a store that cannot be reached throws rather than reporting a failure reason: a store outage
/// offers no reset (FR-017), so an outage is the caller's to report and not this result's to disguise.
/// </remarks>
public sealed record MachineConfigurationResetResult(
    bool Succeeded,
    IReadOnlyList<string> Reset,
    string? FailureReason);
