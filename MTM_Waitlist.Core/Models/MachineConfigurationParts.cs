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
/// <see cref="DisplayName"/> and <see cref="Description"/> are the machine's own configuration rows.
/// <see cref="ScopedPreference"/> is the one setting that belongs to this machine's scope rather than to the
/// plant: a preference row that cannot be repaired is reset to the value the scope resolves without it.
/// </para>
/// <para>
/// Where this machine's pictures come from is deliberately not among them. The shared pictures folder is held
/// once for the whole plant, so no computer captures a folder and there is nothing on a machine to restore
/// (FR-040). The `picture_sources` token an older build accepted is withdrawn with the rows it named.
/// </para>
public static class MachineConfigurationParts
{
    /// <summary>
    /// Every part of this machine's own configuration at once — its display name and its description. It is a
    /// shorthand for naming those two, not a third thing to reset.
    /// </summary>
    public const string Configuration = "machine_configuration";

    /// <summary>The machine's display name, restored to the default the store would give it.</summary>
    public const string DisplayName = "display_name";

    /// <summary>The machine's description.</summary>
    public const string Description = "description";

    /// <summary>A broken preference row belonging to this machine's scope.</summary>
    public const string ScopedPreference = "scoped_preference";
}
