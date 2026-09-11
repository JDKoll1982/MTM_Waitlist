namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// Reads what a launch of the service asked it to show.
/// </summary>
/// <remarks>
/// <para>
/// A bare launch stays tray-only: that is the documented lifetime, it is what logon auto-start does, and
/// the deployment health check asserts it. The window opens only when the command line asks for one,
/// which is how the desktop shortcut's "Show UI" reaches the service.
/// </para>
/// <para>
/// A launch that arrives at an already-running service comes through
/// <c>AppInstance.Activated</c> with the same argument text, so this single parse serves both the start
/// path and the redirect path.
/// </para>
/// </remarks>
internal static class ServiceActivationParser
{
    /// <summary>Asks for the status surface.</summary>
    internal const string StatusSwitch = "--open-status";

    /// <summary>Asks for the settings surface.</summary>
    internal const string SettingsSwitch = "--open-settings";

    /// <summary>What a launch asked the service to show.</summary>
    internal enum RequestedSurface
    {
        /// <summary>Nothing: the service runs tray-only, as it does at logon.</summary>
        None,

        /// <summary>The status surface.</summary>
        Status,

        /// <summary>The settings surface.</summary>
        Settings
    }

    /// <summary>
    /// Reads the requested surface out of a command line.
    /// </summary>
    /// <param name="arguments">The launch arguments, or <see langword="null"/> when there are none.</param>
    /// <returns>The requested surface, or <see cref="RequestedSurface.None"/> when none was asked for.</returns>
    internal static RequestedSurface Parse(string? arguments) =>
        string.IsNullOrWhiteSpace(arguments)
            ? RequestedSurface.None
            : ParseTokens(arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    /// <summary>
    /// Reads the requested surface out of individual arguments.
    /// </summary>
    /// <param name="arguments">
    /// The arguments, including any this service does not know: they are ignored, so an unrelated switch
    /// can never open a window by accident.
    /// </param>
    /// <returns>The requested surface, or <see cref="RequestedSurface.None"/> when none was asked for.</returns>
    internal static RequestedSurface ParseTokens(IEnumerable<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        foreach (var argument in arguments)
        {
            // A shell may pass the switch as a quoted token.
            var token = argument.Trim('"');

            if (string.Equals(token, StatusSwitch, StringComparison.OrdinalIgnoreCase))
            {
                return RequestedSurface.Status;
            }

            if (string.Equals(token, SettingsSwitch, StringComparison.OrdinalIgnoreCase))
            {
                return RequestedSurface.Settings;
            }
        }

        return RequestedSurface.None;
    }
}
