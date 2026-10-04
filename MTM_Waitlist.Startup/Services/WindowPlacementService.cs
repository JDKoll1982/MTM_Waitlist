using Windows.Graphics;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// Why a window's size changed, which decides whether the launch re-centres it
/// (`contracts/window-stage-contract.md` §3; FR-020).
/// </summary>
public enum WindowResizeCause
{
    /// <summary>The launch resized the window as it moved between big phases.</summary>
    Launch,

    /// <summary>The person dragged an edge or a corner, which the launch must not fight (FR-020).</summary>
    Person,
}

/// <summary>
/// A window or display rectangle in physical pixels, kept free of any windowing type so the geometry is testable.
/// </summary>
/// <param name="X">The left edge.</param>
/// <param name="Y">The top edge.</param>
/// <param name="Width">The width.</param>
/// <param name="Height">The height.</param>
public readonly record struct WindowRect(int X, int Y, int Width, int Height)
{
    /// <summary>The horizontal centre of this rectangle.</summary>
    public int CentreX => X + (Width / 2);

    /// <summary>The vertical centre of this rectangle.</summary>
    public int CentreY => Y + (Height / 2);
}

/// <summary>
/// Places the one window: it opens centred on the screen it starts on, re-centres after each resize the launch
/// makes, and maximises when the launch hands over (`contracts/window-stage-contract.md` §3; FR-019, FR-020,
/// FR-021).
/// </summary>
/// <remarks>
/// <para>
/// <b>A resize the person performs is never re-centred.</b> Re-centring exists so a stage change does not leave
/// the window half off the screen; a person who has just placed the window where they want it must not have it
/// moved under them, so only a resize the launch made is answered with a re-centre (FR-020).
/// </para>
/// <para>
/// <b>The display the window is on is the one it is centred within.</b> The window is placed within the bounds of
/// the display it currently sits on, so a machine with two screens centres the launch on the screen it was
/// started on rather than always on the first (FR-021).
/// </para>
/// </remarks>
public sealed class WindowPlacementService
{
    /// <summary>
    /// Whether the launch should re-centre after a resize, which is only for a resize it made itself (FR-020).
    /// </summary>
    /// <param name="cause">Why the size changed.</param>
    public static bool ShouldRecentre(WindowResizeCause cause) => cause is WindowResizeCause.Launch;

    /// <summary>
    /// The position that centres a window of the given size within a display's bounds, clamped so no part of the
    /// window is left outside the display (FR-021).
    /// </summary>
    /// <param name="window">The window's size, whose position is ignored.</param>
    /// <param name="display">The display's bounds, in the same coordinate space.</param>
    /// <returns>The window rectangle centred within the display.</returns>
    public static WindowRect CentreWithin(WindowRect window, WindowRect display)
    {
        if (display.Width <= 0 || display.Height <= 0)
        {
            // A display that answered nothing cannot be centred within, so the window is left where it is rather
            // than moved to a position derived from nonsense.
            return window;
        }

        var width = window.Width > 0 ? Math.Min(window.Width, display.Width) : window.Width;
        var height = window.Height > 0 ? Math.Min(window.Height, display.Height) : window.Height;

        var x = display.X + ((display.Width - width) / 2);
        var y = display.Y + ((display.Height - height) / 2);

        return new WindowRect(x, y, width, height);
    }

    /// <summary>Centres the window on the display it currently sits on (FR-021).</summary>
    /// <param name="appWindow">The window to place.</param>
    /// <exception cref="ArgumentNullException"><paramref name="appWindow"/> is <c>null</c>.</exception>
    public void Centre(Microsoft.UI.Windowing.AppWindow appWindow)
    {
        ArgumentNullException.ThrowIfNull(appWindow);

        var display = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(
            appWindow.Id,
            Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);

        if (display is null)
        {
            return;
        }

        var current = appWindow.Position;
        var size = appWindow.Size;

        var placed = CentreWithin(
            new WindowRect(current.X, current.Y, size.Width, size.Height),
            ToWindowRect(display.OuterBounds));

        appWindow.Move(new PointInt32(placed.X, placed.Y));
    }

    /// <summary>Maximises the window, which is what the move to the shell does (FR-019).</summary>
    /// <param name="appWindow">The window to maximise.</param>
    /// <exception cref="ArgumentNullException"><paramref name="appWindow"/> is <c>null</c>.</exception>
    public void Maximise(Microsoft.UI.Windowing.AppWindow appWindow)
    {
        ArgumentNullException.ThrowIfNull(appWindow);

        if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }
    }

    private static WindowRect ToWindowRect(RectInt32 rect) => new(rect.X, rect.Y, rect.Width, rect.Height);
}
