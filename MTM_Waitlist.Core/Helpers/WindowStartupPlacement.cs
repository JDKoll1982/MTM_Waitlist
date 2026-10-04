using Microsoft.UI.Xaml;

using WinUIEx;

namespace MTM_Waitlist.Module_Core.Helpers;

/// <summary>
/// Where a startup surface opens, so every one of them arrives in the same place.
/// </summary>
/// <remarks>
/// <para>
/// <b>Centred on the display it opens on, not on the primary display.</b> A launch is watched on whichever screen
/// the person is looking at, and the centring call works against the display the window is on, so a workstation
/// with two screens does not put every startup surface on the one the person is not using.
/// </para>
/// <para>
/// <b>A window that could not be centred is still a window.</b> Placement is a cosmetic call, so a failure is
/// recorded and passed over rather than allowed to become a launch failure, exactly as the launch window's own
/// sizing and chrome failures are handled (FR-003).
/// </para>
/// </remarks>
public static class WindowStartupPlacement
{
    /// <summary>Centres the window on the display it opens on.</summary>
    /// <param name="window">The window to place.</param>
    /// <param name="logModule">The module the record is written under, so a failure names which surface it was.</param>
    /// <exception cref="ArgumentNullException"><paramref name="window"/> is <c>null</c>.</exception>
    public static void CentreOnScreen(WindowEx window, string logModule)
    {
        ArgumentNullException.ThrowIfNull(window);

        try
        {
            window.CenterOnScreen();
        }
        catch (Exception exception)
        {
            AppLog.Error(logModule, exception, "The window could not be centred, so it opened where the system put it.");
        }
    }
}
