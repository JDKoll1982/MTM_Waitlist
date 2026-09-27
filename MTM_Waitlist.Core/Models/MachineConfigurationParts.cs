namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// The parts of this machine's configuration a reset may restore, as the tokens accepted by
/// <c>IMachineConfigurationService.ResetToDefaultsAsync</c> and named in
/// <see cref="MachineConfigurationResetResult.Reset"/> (FR-018; contract section 4).
/// </summary>
/// <remarks>
/// <para>
/// A reset restores only what it is told is broken, so the preview the screen shows and the reset that runs
/// describe the same set. Every token here names something on <b>this</b> machine: a reset never touches another
/// machine's configuration, a person, a role, a permission, a session or a log entry.
/// </para>
/// <para>
/// <see cref="DisplayName"/>, <see cref="Description"/> and <see cref="PictureSources"/> are the machine's own
/// configuration rows. <see cref="ScopedPreference"/> is the one setting that belongs to this machine's scope
/// rather than to the plant: a preference row that cannot be repaired is reset to the value the scope resolves
/// without it.
/// </para>
/// </remarks>
public static class MachineConfigurationParts
{
    /// <summary>
    /// Every part of this machine's own configuration at once — its display name, its description and its picture
    /// sources. It is a shorthand for naming those three, not a fourth thing to reset.
    /// </summary>
    public const string Configuration = "machine_configuration";

    /// <summary>The machine's display name, restored to the default the store would give it.</summary>
    public const string DisplayName = "display_name";

    /// <summary>The machine's description.</summary>
    public const string Description = "description";

    /// <summary>The machine's picture sources: the shared picture folder, the keys folder and the dunnage root.</summary>
    public const string PictureSources = "picture_sources";

    /// <summary>A broken preference row belonging to this machine's scope.</summary>
    public const string ScopedPreference = "scoped_preference";
}
