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
    /// A reset of the display name was refused because the default name it would restore is already held by
    /// another machine. Nothing was reset, so the caller can report what is holding the restore up.
    /// </summary>
    public const string DefaultDisplayNameInUse = "default_display_name_in_use";

    /// <summary>
    /// The store was asked to save the machine but does not hold what was asked for when the row is read back,
    /// so the save is reported as not made rather than as made. Success is read back rather than inferred from
    /// a changed-row count, because the store reports zero for a write that changes nothing.
    /// </summary>
    public const string ConfigurationNotWritten = "configuration_not_written";
}
