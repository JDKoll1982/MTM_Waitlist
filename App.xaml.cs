using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Module_Startup.Views;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Notifications;
using MTM_Waitlist.Services.DependencyInjection;

namespace MTM_Waitlist;

public partial class App : Application
{
    private static Microsoft.UI.Dispatching.DispatcherQueue? _uiDispatcher;
    private static WindowEx? _mainWindow;
    private static SplashWindow? _launchWindow;
    private static SignInWindow? _signInWindow;
    private static MachineSetupWindow? _setupWindow;
    private static BlockedStateWindow? _blockedStateWindow;

    public IHost Host
    {
        get;
    }

    public static T GetService<T>() where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }
        return service;
    }

    public static WindowEx MainWindow => _mainWindow ??= new MainWindow();

    /// <summary>
    /// The window the person is currently looking at, or <c>null</c> when no surface is up.
    /// </summary>
    /// <remarks>
    /// A dialog has to be owned by a window, and the one it belongs to is the surface on screen rather than the
    /// shell: during a launch the shell does not exist yet, and a dialog owned by a window the person cannot see
    /// would open behind it or refuse to open at all. The order is newest surface first, because the launch window
    /// stays up until the surface replacing it has been shown.
    /// </remarks>
    internal static WindowEx? CurrentSurfaceWindow =>
        _setupWindow ?? _signInWindow ?? _blockedStateWindow ?? _launchWindow ?? _mainWindow;

    public static UIElement? AppTitlebar
    {
        get; set;
    }

    /// <summary>
    /// Ends the process from whatever thread the caller is on.
    /// </summary>
    /// <remarks>
    /// <c>Application.Exit()</c> must run on the UI thread. Signing out reaches this after an await that
    /// deliberately does not return to the UI thread, so calling <c>Exit()</c> directly left the signed-in
    /// window open beside the replacement instance the sign-out had already launched.
    /// </remarks>
    public static void ExitApplication()
    {
        var dispatcher = _uiDispatcher;

        if (dispatcher is null || dispatcher.HasThreadAccess)
        {
            Current.Exit();
            return;
        }

        _ = dispatcher.TryEnqueue(() => Current.Exit());
    }

    public App()
    {
        AppLog.Info("App", "App constructor started.");

        // Captured here, on the UI thread, so a caller on any other thread can still end the session.
        _uiDispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) => services.AddAppServices(context)).
        Build();

        AppServiceLocator.NavigationService = Host.Services.GetService<INavigationService>();
        SharedServiceLocator.TooltipService = Host.Services.GetService<ITooltipService>();
        SharedServiceLocator.ControlInspectorService = Host.Services.GetService<IControlInspectorService>();

        // Replaces the retired app-lifecycle service. The sign-out path lives in a class library that cannot
        // reference this project, and only the host can marshal the exit onto the UI thread.
        MTM_Waitlist.Module_Startup.Services.AppLifecycleHost.Exit = ExitApplication;

        AppLog.Configure(Host.Services.GetService<ILogService>());
        AppLog.Info("App", "Host built.");

        try
        {
            // The probe driver is what lets the read state leave Unknown and the shell indicator appear
            // (T123). Probing only: refresh scheduling belongs to the on-host service (FR-025), and a probe
            // failure must never block startup.
            App.GetService<IVisualReachabilityProbeHost>().Start();
            AppLog.Info("App", "Visual reachability probe host started.");
        }
        catch (Exception ex)
        {
            AppLog.Error("App", ex, "The Visual reachability probe host failed to start.");
        }

        try
        {
            App.GetService<IAppNotificationService>().Initialize();
            AppLog.Info("App", "App notification service initialized.");
        }
        catch (Exception ex)
        {
            AppLog.Error("App", ex, "App notification service failed to initialize.");
            throw;
        }

        UnhandledException += App_UnhandledException;
        AppLog.Info("App", "UnhandledException handler registered.");

    #if DEBUG
        AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
    #endif
    }

    /// <summary>
    /// Shows the launch window, which is the surface every launch shows while it runs (US1).
    /// </summary>
    /// <remarks>
    /// It is shown before the sequence starts rather than when the sequence ends, because the point of the
    /// surface is that a person can watch a launch that is still happening. A launch that never reaches the main
    /// screens therefore leaves this window in place, which is what the sign-in, setup and blocked surfaces
    /// replace as their phases land.
    /// </remarks>
    private static void ShowLaunchWindow()
    {
        _launchWindow ??= new SplashWindow();
        _launchWindow.Activate();
    }

    /// <summary>
    /// Closes the launch window as part of handing over to the main screens. The window is told it is a hand-over
    /// first, so closing it does not end the process it has just handed to.
    /// </summary>
    private static void CloseLaunchWindow()
    {
        if (_launchWindow is not { } window)
        {
            return;
        }

        _launchWindow = null;
        window.IsHandingOver = true;
        window.Close();
    }

    /// <summary>
    /// Shows the surface a launch ended at, replacing the launch window with it.
    /// </summary>
    /// <remarks>
    /// The launch decides where it ended; the host only shows what that outcome means. The shell is reachable
    /// only from <see cref="LaunchOutcome.MainScreens"/> and machine setup only from
    /// <see cref="LaunchOutcome.MachineSetup"/>, so no outcome shows a surface another outcome owns (S4, FR-006).
    /// The three surfaces a launch can end at replace the launch window rather than sitting over it, so the window
    /// the person is working in is the one that owns the ending and there is no behind-window left to close by
    /// accident. A stop is one of those surfaces: it replaces the launch window and states the cause itself (US4).
    /// <para>
    /// <b>The new surface is shown before the launch window is closed, and the order matters.</b> Closing first
    /// leaves the process with no window at all for as long as it takes to build the next one, and on 2026-09-27
    /// that was long enough for the application to end itself: the first real launch reached machine setup, wrote
    /// "The launch ended at MachineSetup" and then exited with code 0 without ever showing the setup screen. The
    /// same rule is why <c>MachineSetupWindow</c> closes itself only after the launch already has a surface.
    /// </para>
    /// </remarks>
    private static void ShowLaunchSurface(LaunchOutcome outcome)
    {
        switch (outcome)
        {
            case LaunchOutcome.MainScreens:
                MainWindow.Activate();
                CloseLaunchWindow();
                CloseSignInWindow();
                CloseBlockedStateWindow();
                break;

            case LaunchOutcome.SignIn:
                // The sign-in surface is where a launch ends when nobody is signed in yet (FR-001). It replaces the
                // launch window for the same reason setup does: the person works in the window that owns the ending.
                ShowSignInWindow();
                CloseLaunchWindow();
                CloseBlockedStateWindow();
                break;

            case LaunchOutcome.MachineSetup:
                // Setup replaces the launch window rather than sitting over it: the window the person is working in
                // is then the one that owns the ending when they abandon it, and there is no behind-window left to
                // close by accident (FR-008).
                ShowMachineSetupWindow();
                CloseLaunchWindow();
                CloseSignInWindow();
                CloseBlockedStateWindow();
                break;

            case LaunchOutcome.Blocked:
                // A stop is its own surface (US4): it states the cause and offers only the actions that could
                // remove it. A stop that arrives while a stop is already showing reads the lines written since,
                // which is why any earlier one is closed and a fresh one built rather than the old one being woken
                // (FR-004, FR-017). The earlier one goes while the launch window is still up, so the process is
                // never windowless, and the fresh one replaces the launch window in turn.
                CloseBlockedStateWindow();
                ShowBlockedStateWindow();
                CloseLaunchWindow();
                CloseSignInWindow();
                break;

            case LaunchOutcome.Ended:
                ExitApplication();
                break;

            default:
                // A surface no outcome owns: the launch window stays, because leaving the launch running with
                // nothing to report to would be the silent stop SC-001 forbids. There is no such outcome today
                // (FR-001), so this is the guard rather than a route.
                CloseSignInWindow();
                ShowLaunchWindow();
                break;
        }
    }

    /// <summary>
    /// Shows the sign-in window, which is the surface a launch ends at when nobody is signed in yet (FR-001).
    /// </summary>
    /// <remarks>
    /// It is shown on the launch's own decision to end at <see cref="LaunchOutcome.SignIn"/>, so the pipeline
    /// rather than a screen is what decides that a person is needed now. A window that has already closed is
    /// forgotten rather than reactivated, so the next hand-over to this surface builds a fresh one.
    /// </remarks>
    private static void ShowSignInWindow()
    {
        if (_signInWindow is { } existing)
        {
            existing.Activate();
            return;
        }

        var window = new SignInWindow();
        window.Closed += (_, _) => _signInWindow = null;
        _signInWindow = window;
        window.Activate();
    }

    /// <summary>
    /// Closes the sign-in window as part of handing over to another surface, and tells it the close is a hand-over.
    /// </summary>
    /// <remarks>
    /// The window ends the process when the person closes it, because a sign-in window abandoned before anybody
    /// signed in has no surface left to report to (FR-008). This is the host's own close, so the window is told
    /// first for the same reason the launch window is.
    /// </remarks>
    private static void CloseSignInWindow()
    {
        if (_signInWindow is not { } window)
        {
            return;
        }

        _signInWindow = null;
        window.IsHandingOver = true;
        window.Close();
    }

    /// <summary>
    /// Shows the machine-setup window, which is the surface an unconfigured computer is set up on (FR-006).
    /// </summary>
    /// <remarks>
    /// A window that has already closed is forgotten rather than reactivated: setup ends by closing itself, either
    /// because this computer was configured or because the process is going, so the next hand-over to this surface
    /// builds a fresh window instead of waking one that is gone.
    /// </remarks>
    private static void ShowMachineSetupWindow()
    {
        if (_setupWindow is { } existing)
        {
            existing.Activate();
            return;
        }

        var window = new MachineSetupWindow();
        window.Closed += (_, _) => _setupWindow = null;
        _setupWindow = window;
        window.Activate();
    }

    /// <summary>
    /// Shows the blocked-state surface, which is where a stopped launch states its cause and the actions that
    /// could remove it (US4; FR-004, FR-016, FR-017, FR-019).
    /// </summary>
    /// <remarks>
    /// The window decides whether it is needed at all: it offers the fault to the repair policy before it appears,
    /// so a fault that can be put right without asking produces no prompt. The host therefore hands it the launch
    /// and does not wait, and the window activates itself only when the person really has to be asked.
    /// </remarks>
    private static void ShowBlockedStateWindow()
    {
        var window = new BlockedStateWindow();
        window.Closed += (_, _) => _blockedStateWindow = null;
        _blockedStateWindow = window;

        _ = window.StartAsync();
    }

    /// <summary>
    /// Closes the blocked-state surface as part of handing over to another surface, and tells it the close is a
    /// hand-over.
    /// </summary>
    /// <remarks>
    /// A stopped launch the person closes ends the process, because there is no surface left to report to
    /// (FR-008). This is the host's own close, so the window is told first for the same reason the launch window
    /// is.
    /// </remarks>
    private static void CloseBlockedStateWindow()
    {
        if (_blockedStateWindow is not { } window)
        {
            return;
        }

        _blockedStateWindow = null;
        window.IsHandingOver = true;
        window.Close();
    }

    /// <summary>
    /// States the reason the process is ending before it goes, so an abort does not read as a crash (FR-008).
    /// </summary>
    private static void OnLaunchProcessEnding(object? sender, string reason)
    {
        AppLog.Info("Launch", $"The launch is ending: {reason}");
        ExitApplication();
    }

    /// <summary>
    /// Shows the surface the launch handed back, which is the only way the shell is reached (S4).
    /// </summary>
    /// <remarks>
    /// <b>The hand-over is marshalled onto the UI thread, and it has to be.</b> The launch runs its steps with
    /// <c>ConfigureAwait(false)</c> on purpose, so a slow store never blocks the interface, which means this event
    /// is raised on a thread-pool thread. Every surface it leads to is a window: building, activating or closing
    /// one from any other thread fails, and it fails quietly here because the task the launch runs on is one the
    /// host deliberately does not await. On 2026-09-27 that left the first real launch sitting on the launch
    /// window with its feed complete and "The launch ended at MachineSetup" in the log, which is a silent stop of
    /// exactly the kind SC-001 forbids.
    /// </remarks>
    private static void OnLaunchShellReady(object? sender, LaunchOutcome outcome)
    {
        AppLog.Info("Launch", $"The launch ended at {outcome}.");
        RunOnUiThread(() => HandOverToSurface(outcome));
    }

    /// <summary>
    /// Builds and shows the surface an outcome means, and refuses to let a surface failure end the process.
    /// </summary>
    /// <param name="outcome">Where the launch ended.</param>
    /// <remarks>
    /// A window that cannot be built is a fault in this application, not a reason for it to vanish: the process
    /// going away without a word is the silent stop SC-001 forbids, and it takes the launch window with it, which
    /// is the surface a person would have read the feed on. So the failure is recorded with its exception and the
    /// launch surface is put back, leaving the person the lines the launch already wrote.
    /// </remarks>
    private static void HandOverToSurface(LaunchOutcome outcome)
    {
        try
        {
            ShowLaunchSurface(outcome);
        }
        catch (Exception exception)
        {
            AppLog.Error("Launch", exception, $"The surface for {outcome} could not be shown, so the launch window is staying.");
            ShowLaunchWindow();
        }
    }

    /// <summary>
    /// Runs work on the interface thread, directly when the caller is already on it.
    /// </summary>
    /// <param name="work">The work that touches a window.</param>
    private static void RunOnUiThread(Action work)
    {
        var dispatcher = _uiDispatcher;

        if (dispatcher is null || dispatcher.HasThreadAccess)
        {
            work();
            return;
        }

        _ = dispatcher.TryEnqueue(() => work());
    }

    /// <summary>
    /// Stops the host so no background service outlives the last window.
    /// </summary>
    public static Task ShutdownHostAsync() =>
        (Current as App)?.ShutdownAsync() ?? Task.CompletedTask;

    private async Task ShutdownAsync()
    {
        AppLog.Info("Shutdown", "Shutdown started.");
        try
        {
            App.GetService<IAppNotificationService>().Unregister();
            AppLog.Info("Shutdown", "App notification service unregistered.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Shutdown", ex, "Failed to unregister app notification service.");
        }

        try
        {
            await Host.StopAsync().ConfigureAwait(false);
            AppLog.Info("Shutdown", "Host stopped.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Shutdown", ex, "Failed to stop host.");
        }
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        if (e.Exception is not null)
        {
            AppLog.Error("UnhandledException", e.Exception, $"Unhandled exception message: {e.Message}");
            return;
        }

        AppLog.Info("UnhandledException", $"Unhandled exception message: {e.Message}");
    }

    private static void CurrentDomain_FirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
    {
#if DEBUG
        if (e.Exception is COMException comException)
        {
            var stack = comException.StackTrace ?? string.Empty;
            if (stack.Contains("MTM_Waitlist", StringComparison.OrdinalIgnoreCase))
            {
                AppLog.Error("FirstChance", comException, $"First-chance COMException in MTM_Waitlist stack. HResult=0x{comException.HResult:X8}.");
            }
        }

        if (e.Exception is NullReferenceException nullReferenceException)
        {
            var stack = nullReferenceException.StackTrace ?? string.Empty;
            if (stack.Contains("MTM_Waitlist", StringComparison.OrdinalIgnoreCase))
            {
                AppLog.Error("FirstChance", nullReferenceException, "First-chance NullReferenceException in MTM_Waitlist stack.");
            }
        }

        if (e.Exception is FileNotFoundException fileNotFoundException)
        {
            var stack = fileNotFoundException.StackTrace ?? string.Empty;
            if (stack.Contains("MTM_Waitlist", StringComparison.OrdinalIgnoreCase)
                || stack.Contains("Tooltip", StringComparison.OrdinalIgnoreCase)
                || stack.Contains("ResourceLoader", StringComparison.OrdinalIgnoreCase)
                || stack.Contains("ResourceManager", StringComparison.OrdinalIgnoreCase))
            {
                var fileName = string.IsNullOrWhiteSpace(fileNotFoundException.FileName)
                    ? "<unknown>"
                    : fileNotFoundException.FileName;
                AppLog.Error(
                    "FirstChance",
                    fileNotFoundException,
                    $"First-chance FileNotFoundException. FileName='{fileName}'.");
            }
        }
#endif
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        AppLog.Info("Launch", "OnLaunched started.");
        base.OnLaunched(args);

        try
        {
            // Starts the generic background thread host runtime manager engine
            await Host.StartAsync();
            AppLog.Info("Launch", "Host started.");

            // The launch surface is shown before the sequence starts, so a person watches the launch rather than
            // waiting on a window that has not appeared yet (US1).
            ShowLaunchWindow();

            // The host owns one call and two events. Which surface the person is shown is the launch's decision,
            // handed back through ShellReady, and the reason an ending is stated through ProcessEnding before the
            // process goes (S4, FR-008). The host no longer hands windows to each other.
            var pipeline = App.GetService<ILaunchPipeline>();
            pipeline.ShellReady += OnLaunchShellReady;
            pipeline.ProcessEnding += OnLaunchProcessEnding;

            _ = pipeline.RunAsync(CancellationToken.None);
            AppLog.Info("Launch", "The launch was started.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Launch", ex, "Unhandled exception during launch pipeline.");
            throw;
        }
    }
}
