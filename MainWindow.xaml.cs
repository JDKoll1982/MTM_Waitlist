using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Options;
using MTM_Waitlist.Module_Startup.Services;
using Windows.Graphics;
using Windows.UI.ViewManagement;

namespace MTM_Waitlist;

public sealed partial class MainWindow : WindowEx
{
    /// <summary>Where the launch surface opens at, before the configured size could be read.</summary>
    private static readonly SizeInt32 DefaultLaunchSize = new(460, 420);

    private readonly Microsoft.UI.Dispatching.DispatcherQueue? _dispatcherQueue;
    private readonly UISettings? _settings;

    /// <summary>Places the one window: centred while it hosts a launch stage, maximised for the shell.</summary>
    private readonly WindowPlacementService _placement = new();

    private bool _isDisposed;

    public MainWindow()
    {
        InitializeComponent();
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/Packaging/window-icon.ico"));
        Title = "AppDisplayName".GetLocalized();

        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        // The window's close box is a route out of every launch stage, and each stage states its own reason for
        // the ending. The host already cancelled the close, so the window does not vanish before the reason has
        // been stated (FR-008).
        AppWindow.Closing += OnWindowClosing;

        try
        {
            _settings = new UISettings();
            _settings.ColorValuesChanged += Settings_ColorValuesChanged;
        }
        catch (Exception)
        {
            _settings = null;
        }

        Closed += MainWindow_Closed;
    }

    /// <summary>
    /// Shows a launch stage, or the shell, as the whole content of the one window
    /// (`contracts/window-stage-contract.md` §§1-3; FR-016, FR-019, FR-020, FR-023).
    /// </summary>
    /// <param name="stage">The stage the launch is at, which decides the window's size and whether it maximises.</param>
    /// <param name="content">The stage's own content: a launch surface while the launch runs, the shell at hand-over.</param>
    /// <remarks>
    /// <para>
    /// <b>The window resizes between the big phases and re-centres after each resize it makes.</b> A resize the
    /// person performs does not come through here, so it is never re-centred and the launch never fights the
    /// person's own placement (FR-020).
    /// </para>
    /// <para>
    /// <b>It maximises only on the move to the shell.</b> Which stages maximise is
    /// <see cref="LaunchStageSelection.Maximises"/>, never a list repeated here, so the rule has one account.
    /// </para>
    /// <para>
    /// <b>A window that cannot be resized is still a window that can show a launch.</b> Every window-styling
    /// failure is recorded and the stage is shown at the size the window already had, because a sizing failure
    /// must never become a launch failure.
    /// </para>
    /// </remarks>
    public void ShowStage(LaunchStage stage, UIElement content)
    {
        ArgumentNullException.ThrowIfNull(content);

        StageHost.Content = content;

        if (LaunchStageSelection.Maximises(stage))
        {
            try
            {
                _placement.Maximise(AppWindow);
            }
            catch (Exception exception)
            {
                // A window that cannot be maximized is still a window that can show the shell, so the hand-over
                // carries on at the size the window opened at rather than stopping (FR-019).
                AppLog.Error(
                    "MainWindow",
                    exception,
                    "The main window could not be maximized, so it is keeping the size it opened at.");
            }

            return;
        }

        ResizeFor(stage);
        _placement.Centre(AppWindow);
    }

    /// <summary>
    /// Resizes the window to the stage's own size, and treats every failure as a sizing failure rather than a
    /// launch failure.
    /// </summary>
    private void ResizeFor(LaunchStage stage)
    {
        try
        {
            AppWindow.Resize(SizeFor(stage));
        }
        catch (Exception exception)
        {
            AppLog.Error(
                "MainWindow",
                exception,
                "The window could not be resized for the launch stage, so it is keeping the size it had.");
        }
    }

    /// <summary>
    /// The size each launch stage reads at, which is the smallest each surface holds its own content at. The
    /// splash uses the configured launch size so a workstation can still be given a different one (checklist
    /// 8.2c); the shell maximises rather than being sized here.
    /// </summary>
    private static SizeInt32 SizeFor(LaunchStage stage) => stage switch
    {
        LaunchStage.SignIn => new SizeInt32(460, 500),
        LaunchStage.MachineSetup => new SizeInt32(520, 560),
        LaunchStage.Blocked => new SizeInt32(480, 420),
        LaunchStage.Splash => ConfiguredLaunchSize(),
        _ => DefaultLaunchSize,
    };

    /// <summary>
    /// The configured launch size, or the surface's own default when no usable setting was supplied. A window the
    /// person can still read beats refusing to size over a mistyped figure (checklist 8.2c).
    /// </summary>
    private static SizeInt32 ConfiguredLaunchSize()
    {
        try
        {
            var options = App.GetService<IOptions<LaunchWindowOptions>>().Value;

            return options is { IsUsable: true }
                ? new SizeInt32(options.Width, options.Height)
                : DefaultLaunchSize;
        }
        catch (Exception)
        {
            return DefaultLaunchSize;
        }
    }

    /// <summary>
    /// Turns the one window's close box into the launch-endings route while a launch stage is showing, and lets
    /// the window close normally once the shell owns it (FR-008).
    /// </summary>
    /// <remarks>
    /// The close is cancelled and the stage answers it, so a window closed mid-launch states why before the
    /// process goes rather than vanishing into what reads as a crash. Once the launch has handed over, or once
    /// the process has already been asked to end, the close is left alone.
    /// </remarks>
    private void OnWindowClosing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        if (App.HasHandedOverToShell || App.IsExitRequested)
        {
            return;
        }

        args.Cancel = true;

        App.OnMainWindowClosingDuringLaunch();
    }

    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        if (_isDisposed)
        {
            return;
        }

        // FIX 1: Explicitly check if the captured dispatcher queue is active and accepting work
        if (_dispatcherQueue is null || _dispatcherQueue.HasThreadAccess == false && _isDisposed)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            if (_isDisposed)
            {
                return;
            }
            TitleBarHelper.ApplySystemThemeToCaptionButtons(App.MainWindow, App.AppTitlebar);
        });
    }

    private void MainWindow_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        // FIX 2: Set the disposal barrier flag first before anything else executes
        _isDisposed = true;

        if (_settings is not null)
        {
            try
            {
                _settings.ColorValuesChanged -= Settings_ColorValuesChanged;
            }
            catch
            {
                // Guard against native shell registration crashes during rapid app exit routines
            }
        }

        Closed -= MainWindow_Closed;
    }
}
