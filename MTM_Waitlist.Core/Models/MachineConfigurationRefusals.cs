namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// Why a save or a reset was refused, as the stable token
/// <see cref="MachineConfigurationSaveResult.RefusalReason"/> and
/// <see cref="MachineConfigurationResetResult.FailureReason"/> carry.
/// </summary>
/// <remarks>
/// These are answers, not faults: each one names something the person can act on or that the caller can report as
/// a specific outcome. A store that cannot be reached is deliberately not among them — it throws, because an
/// outage is not something the operator's answer caused and must not be reported as though it were.
/// </remarks>
public static class MachineConfigurationRefusals
{
    /// <summary>A display name of nothing but whitespace is refused: the machine needs a name people recognise.</summary>
    public const string DisplayNameRequired = "display_name_required";

    /// <summary>
    /// Another machine already holds this display name. It is refused here rather than left to collide on the
    /// store's unique key, so the screen can ask for a different name.
    /// </summary>
    public const string DisplayNameInUse = "display_name_in_use";

    /// <summary>
    /// The draft does not carry all three picture sources, or one of them is blank. All three are required, so
    /// this is refused as an incomplete save rather than written as two of three.
    /// </summary>
    public const string PictureSourcesIncomplete = "picture_sources_incomplete";

    /// <summary>
    /// The store accepted the display name and the description but did not write all three picture-source rows,
    /// so the machine is not configured and nothing else is reported as saved.
    /// </summary>
    public const string PictureSourcesNotWritten = "picture_sources_not_written";

    /// <summary>
    /// A reset of the display name was refused because the default name it would restore is already held by
    /// another machine. Nothing was reset, so the caller can report what is holding the restore up.
    /// </summary>
    public const string DefaultDisplayNameInUse = "default_display_name_in_use";
}
