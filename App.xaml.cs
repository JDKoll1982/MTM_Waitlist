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
    private static FrameworkElement? _shellContent;
    private static SplashWindow? _launchWindow;
    private static SignInWindow? _signInWindow;
    private static MachineSetupWindow? _setupWindow;
    private static BlockedStateWindow? _blockedStateWindow;

    /// <summary>How many faults this process has written down, so a fault inside a loop cannot fill the disk.</summary>
    private static int s_recordedFaults;

    /// <summary>Whether the process has already been asked to end, so an ending is asked for and performed once.</summary>
    private static int s_exitRequested;

    /// <summary>The most fault records one process writes, which is far more than any single fault needs.</summary>
    private const int MaximumRecordedFaults = 25;

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
    /// Ends the process from whatever thread the caller is on, once the store has received the reason.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Application.Exit()</c> must run on the UI thread. Signing out reaches this after an await that
    /// deliberately does not return to the UI thread, so calling <c>Exit()</c> directly left the signed-in
    /// window open beside the replacement instance the sign-out had already launched.
    /// </para>
    /// <para>
    /// <b>Until 2026-10-01 the reason the process was going never reached the store.</b> A log entry returns as
    /// soon as it is queued and <c>Exit()</c> takes the queue with it, so an ending that was asked for one line
    /// after the reason was stated left the store holding everything except that line. That is what both runs of
    /// that day look like: a launch that stops mid-step, this process's connections dropped a moment later, and
    /// no account anywhere of who ended it or why — which is indistinguishable from the silent stop FR-004 and
    /// FR-008 forbid. The ending therefore states itself durably and waits, within the log seam's own bound, for
    /// that statement to land before the process goes.
    /// </para>
    /// </remarks>
    public static void ExitApplication()
    {
        // Asked once. A second request is a route that has already been answered, and ending twice would take a
        // second pass through the flush for nothing.
        if (Interlocked.Exchange(ref s_exitRequested, 1) == 1)
        {
            return;
        }

        AppLog.Info(
            "App",
            "The process is ending: the lines already raised, including the reason, are being written to the store first.");

        _ = LeaveAsync();
    }

    /// <summary>
    /// Waits, within the log seam's own bound, for the lines already raised to reach the store, and then ends the
    /// process.
    /// </summary>
    private static async Task LeaveAsync()
    {
        try
        {
            if ((Current as App)?.Host.Services.GetService<ILogService>() is { } log)
            {
                await log.FlushAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            // A store that will not take the reason is not a reason to stay: the process was asked to end, so it
            // ends, with the failure recorded the only way left to record it.
            AppLog.Error("App", exception, "The lines raised before the process ended could not be written to the store.");
        }

        RunOnUiThread(() => Current.Exit());
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

        // A fault on a thread that is not the interface thread never reaches UnhandledException, and it ends the
        // process just the same. On 2026-10-01 a sign-in that changed a temporary credential reached the main
        // screens and the process went half a second later: the store held its last successful line, the store
        // host recorded the pooled connections being dropped, and there was no fault record anywhere, because
        // this route had none. It gets the same record as the interface thread's, since that record is the only
        // one that outlives the process it describes.
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        AppLog.Info("App", "Background-thread fault handler registered.");

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
                ShowShellContent();
                MaximizeMainWindow();
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
    /// Puts the shell into the main window, once, the first time a launch reaches the main screens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The window declares no content of its own, so this is the only thing that can fill it.</b>
    /// <c>MainWindow.xaml</c> is a window and nothing else, and the shell is built here rather than in the host's
    /// constructor so it does not exist until the launch routes to it (FR-008). The one page is kept because the
    /// provider creates a new one per call: a second call would build a second shell and lose the person's place
    /// in the first.
    /// </para>
    /// <para>
    /// <b>Without this the main screens are a blank window.</b> Nothing else assigned the content, so a launch
    /// that ended at <see cref="LaunchOutcome.MainScreens"/> activated a window with nothing in it and no page
    /// ever asked the store for anything — which is what the store's log showed on 2026-09-28, and what the
    /// person saw.
    /// </para>
    /// </remarks>
    private static void ShowShellContent()
    {
        if (_shellContent is not null)
        {
            return;
        }

        _shellContent = App.GetService<IShellContentProvider>().CreateShellContent();
        MainWindow.Content = _shellContent;
    }

    /// <summary>
    /// Opens the main window maximized, which is the size the shell is designed for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Maximized before the window is activated, so it never appears small first.</b> The overlapped
    /// presenter's state is what the system reads while it is showing the window, so setting it here is what makes
    /// the window arrive maximized instead of resizing in front of the person a moment later.
    /// </para>
    /// <para>
    /// <b>A window that cannot be maximized is still a window that can show the shell.</b> A failure is recorded
    /// and the hand-over carries on with the window at the size it opened at, which is the fallback the launch
    /// contract asks for when it says a window that cannot be maximized falls back to the configured main size and
    /// the transition still completes. Nothing here is allowed to throw, for that reason.
    /// </para>
    /// </remarks>
    private static void MaximizeMainWindow()
    {
        try
        {
            MainWindow.Maximize();
        }
        catch (Exception exception)
        {
            AppLog.Error(
                "App",
                exception,
                "The main window could not be maximized, so it is keeping the size it opened at.");
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

    /// <summary>
    /// Records a fault that is about to end the process, and does not swallow it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The fault is not handled.</b> This handler records it and returns, so the process goes on to end exactly
    /// as it would have. Marking it handled would leave the person looking at a screen the application can no
    /// longer draw, which is worse than a stop that says what happened.
    /// </para>
    /// <para>
    /// <b>The record has to outlive the process, and the store log cannot.</b> Every line written to the store
    /// goes through a queue that another thread flushes, so a fault that ends the process takes the account of
    /// itself with it. On 2026-09-28 the application died as the main screens loaded (fault bucket
    /// 1839341221626038306, a stowed <c>InvalidOperationException</c> reported as 0x80131509) and the store held
    /// nothing but successful lines, which is what made the cause impossible to read. This is the record that
    /// survives the process, so the next one can be read.
    /// </para>
    /// </remarks>
    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        if (e.Exception is not null)
        {
            AppLog.Error("UnhandledException", e.Exception, $"Unhandled exception message: {e.Message}");
        }
        else
        {
            AppLog.Info("UnhandledException", $"Unhandled exception message: {e.Message}");
        }

        RecordFault("Unhandled", e.Exception is null ? e.Message : e.Exception.ToString());
    }

    /// <summary>
    /// Records a fault raised on a thread that is not the interface thread, which is about to end the process.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the route a launch dies on without a word.</b> Every fault raised on the interface thread reaches
    /// <see cref="App_UnhandledException"/>; a fault raised anywhere else — a hosted service, a task the launch
    /// does not await, a store callback — ends the process through this one instead, and the store log cannot
    /// account for it because the queue is flushed by a thread that is going away with it. The record written here
    /// is the only account of that death, so it is written before the process goes.
    /// </para>
    /// <para>
    /// The fault is not handled, and it cannot be: the process is already ending. Nothing is swallowed here either,
    /// so the runtime still reports what it reported before this handler existed.
    /// </para>
    /// </remarks>
    private static void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        var fault = e.ExceptionObject?.ToString() ?? "<the runtime supplied no exception object>";

        AppLog.Error(
            "UnhandledException",
            e.ExceptionObject as Exception,
            $"A fault on a thread that is not the interface thread is ending the process. Terminating={e.IsTerminating}.");

        RecordFault("Unhandled (background thread)", $"Terminating={e.IsTerminating}{Environment.NewLine}{fault}");
    }

    /// <summary>Writes a fault to a file that outlives the process that recorded it.</summary>
    /// <param name="where">Which handler recorded it, so a first-chance line is not mistaken for the fatal one.</param>
    /// <param name="detail">The fault in full, including its stack.</param>
    /// <remarks>
    /// Best-effort: a fault that cannot be written down must not become a second fault on top of the one being
    /// recorded, so nothing here is allowed to throw. The files accumulate under the application's own local
    /// folder rather than being overwritten, because the fault before the last one is often the interesting one.
    /// </remarks>
    private static void RecordFault(string where, string detail)
    {
        if (Interlocked.Increment(ref s_recordedFaults) > MaximumRecordedFaults)
        {
            return;
        }

        try
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MTM_Waitlist",
                "faults");
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, $"fault-{DateTime.Now:yyyyMMdd-HHmmss-fff}.txt");
            File.WriteAllText(
                file,
                $"{DateTimeOffset.Now:O} {where}{Environment.NewLine}{detail}{Environment.NewLine}");
        }
        catch (Exception)
        {
            // Writing the record is best-effort, and a failure here is deliberately not reported anywhere.
        }
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

        // An InvalidOperationException out of the dispatcher reaches the fault record as a stowed fault with no
        // account of itself, because a fault that ends the process takes the store log's unsent lines with it.
        // Recording it while it is still first-chance is what keeps the message and the stack readable afterwards.
        if (e.Exception is InvalidOperationException invalidOperationException)
        {
            var stack = invalidOperationException.StackTrace ?? string.Empty;
            if (stack.Contains("MTM_Waitlist", StringComparison.OrdinalIgnoreCase))
            {
                AppLog.Error("FirstChance", invalidOperationException, "First-chance InvalidOperationException in MTM_Waitlist stack.");
                RecordFault("FirstChance", invalidOperationException.ToString());
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
