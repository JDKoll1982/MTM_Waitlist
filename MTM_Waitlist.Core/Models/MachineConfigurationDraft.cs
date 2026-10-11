namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// What machine setup captured on this machine, ready to be written (FR-007, FR-018).
/// </summary>
/// <param name="ComputerName">
/// The name the computer reports for itself, read from the machine and shown rather than typed. The store holds
/// it uniquely, so a name another record already uses is refused by the save rather than written.
/// </param>
/// <param name="Location">Where this computer stands. It is the only value a person supplies at setup.</param>
/// <remarks>
/// Identity and nothing else: where this machine's pictures come from is held once for the whole plant and
/// changed from the settings panel, so no computer captures a folder and there is none to write here (FR-040).
/// </remarks>
public sealed record MachineConfigurationDraft(
    string ComputerName,
    string Location);
