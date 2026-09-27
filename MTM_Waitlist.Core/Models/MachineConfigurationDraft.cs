namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// What machine setup captured on this machine, ready to be written (FR-007, FR-018).
/// </summary>
/// <param name="DisplayName">
/// The name people recognise the machine by. The store holds it uniquely, so a name another machine already uses
/// is refused by the save rather than written.
/// </param>
/// <param name="Description">A short note about the machine. May be blank.</param>
/// <param name="PictureSources">
/// The machine's three picture sources. All three are required — the shared picture folder, the keys folder and
/// the dunnage root — because a machine holding two of them is configured wrongly rather than half-configured,
/// and a save that omits one is refused instead of written.
/// </param>
public sealed record MachineConfigurationDraft(
    string DisplayName,
    string Description,
    IReadOnlyList<PictureSource> PictureSources);
