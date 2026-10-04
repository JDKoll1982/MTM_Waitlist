namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// What machine setup captured on this machine, ready to be written (FR-007, FR-018).
/// </summary>
/// <param name="DisplayName">
/// The name people recognise the machine by. The store holds it uniquely, so a name another machine already uses
/// is refused by the save rather than written.
/// </param>
/// <param name="Description">A short note about the machine. May be blank.</param>
/// <remarks>
/// Identity and nothing else: where this machine's pictures come from is held once for the whole plant and
/// changed from the settings panel, so no computer captures a folder and there is none to write here (FR-040).
/// </remarks>
public sealed record MachineConfigurationDraft(
    string DisplayName,
    string Description);
