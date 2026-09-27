namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// The four ways a machine can be unconfigured, as the stable tokens
/// <see cref="MachineConfigurationState.UnconfiguredReason"/> carries (FR-004, FR-009).
/// </summary>
/// <remarks>
/// The tokens are values rather than sentences: the screen says them in the reader's own words, and the token
/// itself never changes when the wording does. All four resolve to <c>IsConfigured == false</c>, which is the
/// point — losing configuration never degrades into running without it (FR-009).
/// </remarks>
public static class MachineConfigurationReasons
{
    /// <summary>
    /// The machine has never been given its configuration. Its registry row may exist — a machine can be known to
    /// the registry without ever having been set up — but it holds no picture sources at all, so nothing was ever
    /// configured for it. This is the ordinary state of a machine arriving on site.
    /// </summary>
    public const string NeverConfigured = "never_configured";

    /// <summary>
    /// The machine was configured and its configuration is no longer complete: its picture sources have been
    /// withdrawn, or it holds fewer than the three it needs, or its display name has been cleared.
    /// </summary>
    public const string Removed = "configuration_removed";

    /// <summary>
    /// The machine's registry row has been retired (<c>core_computers_registry.is_registered = 0</c>). The row
    /// survives and its configuration with it, but a revoked machine is not configured for use.
    /// </summary>
    public const string Revoked = "configuration_revoked";

    /// <summary>
    /// The store could not be read, so there is no verdict about the machine — only that it cannot be shown to be
    /// configured. It is reported as unconfigured rather than assumed configured, and it is a different answer
    /// from the other three because a read that failed may succeed on the next check.
    /// </summary>
    public const string Unreadable = "configuration_unreadable";
}
