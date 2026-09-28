using Microsoft.Extensions.Options;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Startup.Options;

using Windows.Graphics;

namespace MTM_Waitlist.Module_Startup.Views;

/// <summary>
/// The launch window: it shows the launch while it runs, line by line, and states the cause of a stop
/// (US1; FR-002, FR-004, FR-005).
/// </summary>
/// <remarks>
/// <para>
/// <b>Its size is configuration, and a sizing failure is not a launch failure.</b> The size is read from
/// <see cref="LaunchWindowOptions"/> and applied through the window's own <c>AppWindow</c>. Anything at all going
/// wrong leaves the window at the size its markup declares and the launch carries on, because a window that
/// cannot be resized is still a window that can show a launch.
/// </para>
/// <para>
/// <b>Closing it ends the process, unless the launch is handing over.</b> A launch window closed while the launch
/// is still running has no surface left to report to, so the host is stopped and the process ends rather than
/// leaving an invisible one holding store connections. When the launch has reached the main screens the host
/// closes this window as part of the hand-over, and that close must not end the process, which is what
/// <see cref="IsHandingOver"/> records.
/// </para>
/// </remarks>
public sealed partial class SplashWindow : WindowEx
{
    /// <summary>Guards the ending, so the person's close and the hand-over cannot both run it.</summary>
    private static bool s_isEnding;

    public SplashWindow()
    {
        InitializeComponent();

        ApplyConfiguredSize();
        ApplyChrome();
        CentreOnScreen();

        Closed += OnLaunchWindowClosed;
    }

    /// <summary>
    /// Puts the launch surface in the middle of the display it opened on (FR-045).
    /// </summary>
    /// <remarks>
    /// A window that cannot be centred is still a window that can show a launch, so a failure here is recorded and
    /// the launch carries on, exactly as a sizing failure is.
    /// </remarks>
    private void CentreOnScreen()
    {
        try
        {
            this.CenterOnScreen();
        }
        catch (Exception exception)
        {
            AppLog.Error(
                "StartupLaunch",
                exception,
                "The launch window could not be centred, so it opened where the system put it.");
        }
    }

    /// <summary>
    /// Takes the system frame off the launch surface, so it reads as a splash rather than as a small window.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The presenter is the only thing that can do this.</b> A title bar cannot hide itself: the system keeps
    /// control of the caption buttons for as long as one is drawn, and there is no property on
    /// <c>AppWindowTitleBar</c> that removes it. The one call that does is the overlapped presenter's, and both
    /// parts have to be false — a window that keeps its border keeps a caption with it, which is why the frame was
    /// still there when only the title bar was asked for.
    /// </para>
    /// <para>
    /// <b>No frame means no drag area and no system buttons.</b> The surface therefore carries its own way out,
    /// which is the button in its own button bar.
    /// </para>
    /// <para>
    /// <b>A frame that cannot be removed is not a launch failure.</b> These are window-styling calls, and a
    /// surface that still shows the launch with a title bar on it is worth more than a launch that refuses to
    /// start.
    /// </para>
    /// </remarks>
    private void ApplyChrome()
    {
        try
        {
            if (AppWindow.Presenter is not OverlappedPresenter presenter)
            {
                return;
            }

            presenter.IsResizable = false;
            presenter.IsMinimizable = false;
            presenter.IsMaximizable = false;
            presenter.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);
        }
        catch (Exception exception)
        {
            AppLog.Error(
                "StartupLaunch",
                exception,
                "The launch window's frame could not be removed, so it opened with its title bar still shown.");
        }
    }

    /// <summary>
    /// Whether the host is closing this window because the launch reached the main screens. The host sets it
    /// immediately before closing, and a close that carries it is a hand-over rather than an abandonment.
    /// </summary>
    public bool IsHandingOver { get; set; }

    /// <summary>
    /// Applies the configured size, and treats every failure as a sizing failure rather than a launch failure.
    /// </summary>
    private void ApplyConfiguredSize()
    {
        try
        {
            var options = App.GetService<IOptions<LaunchWindowOptions>>().Value;

            if (options is null || !options.IsUsable)
            {
                // No usable setting, so the size the markup declares stands. A window the person can still read
                // beats refusing to open over a mistyped figure.
                return;
            }

            AppWindow.Resize(new SizeInt32(options.Width, options.Height));
        }
        catch (Exception exception)
        {
            AppLog.Error(
                "StartupLaunch",
                exception,
                "The launch window's configured size could not be applied, so it opened at its default size.");
        }
    }

    /// <summary>
    /// Ends the process when the person closes the launch window, and stays out of the way when the host closes
    /// it to hand over to the main screens.
    /// </summary>
    private async void OnLaunchWindowClosed(object sender, WindowEventArgs args)
    {
        if (IsHandingOver)
        {
            return;
        }

        await EndProcessAsync("Startup_Launch.EndedByWindowClose".GetLocalized());
    }

    /// <summary>
    /// Stops the host and ends the process, stating why. Bounded and idempotent: the host stop is awaited so no
    /// background service is still writing when the process goes away, and a second caller is a no-op.
    /// </summary>
    /// <param name="reason">
    /// Why the process is ending, in the reader's words. It is the same ending whichever route reached it, so the
    /// surface's own button and the window's close answer alike.
    /// </param>
    internal static async Task EndProcessAsync(string reason)
    {
        if (s_isEnding)
        {
            return;
        }

        s_isEnding = true;

        AppLog.Info("StartupLaunch", reason);
        await App.ShutdownHostAsync().ConfigureAwait(true);
        App.ExitApplication();
    }
}
