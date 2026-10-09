using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Services;
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
    private static MainWindow? _mainWindow;
    private static FrameworkElement? _shellContent;

    /// <summary>The stage's own surface, when it is one that answers the window's close (FR-008).</summary>
    private static ILaunchStageSurface? _launchStageSurface;

    /// <summary>Whether the launch has handed over to the shell, after which the window closes normally.</summary>
    private static bool s_launchHandedOver;

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

    public static MainWindow MainWindow => _mainWindow ??= new MainWindow();

    /// <summary>
    /// The one window for the whole life of the application, or <c>null</c> before it has been built.
    /// </summary>
    /// <remarks>
    /// There is one window and no second one (FR-016, FR-043). A dialog has to be owned by a window, so this is
    /// the window it belongs to whatever stage is showing, because the stage the person is looking at is content
    /// inside this window rather than a window of its own.
    /// </remarks>
    internal static WindowEx? CurrentSurfaceWindow => _mainWindow;

    /// <summary>
    /// Whether the launch has handed over to the shell, after which closing the window is an ordinary close
    /// rather than a route out of a launch stage (FR-008, FR-022).
    /// </summary>
    internal static bool HasHandedOverToShell => s_launchHandedOver;

    /// <summary>
    /// Whether the process has already been asked to end, so a close that is part of the ending is not mistaken
    /// for a route out of a launch stage.
    /// </summary>
    internal static bool IsExitRequested => Volatile.Read(ref s_exitRequested) == 1;

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

        // How much the store keeps is a plant-wide setting, so it is applied here rather than only when somebody
        // opens Settings: a machine has to keep what the plant decided from its first entry, not from its first
        // visit to a screen. The read is one configuration row and is deliberately not awaited — the shipped
        // default is already in force, and a launch must never wait on the diagnostics it is recording.
        AdoptStoredLogStoreLevel(Host.Services);

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
    /// Shows the launch stage, which is what the one window shows while a launch runs
    /// (`contracts/window-stage-contract.md` §2; US1, FR-016).
    /// </summary>
    /// <remarks>
    /// It is shown before the sequence starts rather than when the sequence ends, because the point of the
    /// surface is that a person can watch a launch that is still happening. A launch that never reaches the main
    /// screens therefore leaves this stage in place, and the host replaces it with the stage the outcome means as
    /// each phase lands.
    /// </remarks>
    private static void ShowLaunchWindow() => ShowLaunchStage(LaunchStage.Splash, new SplashPage());

    /// <summary>
    /// Shows one launch stage as the whole content of the one window, and remembers it so the window's close box
    /// can be answered by the stage that is showing (`contracts/window-stage-contract.md` §§1-2; FR-016, FR-022).
    /// </summary>
    /// <typeparam name="T">The stage's own surface type, which is also its content.</typeparam>
    /// <param name="stage">The stage to show.</param>
    /// <param name="surface">The stage's surface, which is what fills the window.</param>
    private static void ShowLaunchStage<T>(LaunchStage stage, T surface)
        where T : UIElement, ILaunchStageSurface
    {
        _launchStageSurface = surface;

        MainWindow.ShowStage(stage, surface);
        MainWindow.Activate();
    }

    /// <summary>
    /// Shows the stage the launch ended at, as the whole content of the one window
    /// (`contracts/window-stage-contract.md` §2; S4, FR-006, FR-016, FR-022).
    /// </summary>
    /// <remarks>
    /// The launch decides where it ended; the host only shows what that outcome means, and the mapping from
    /// outcome to stage is <see cref="LaunchStageSelection"/>'s single answer rather than a list repeated here. The
    /// shell is reachable only from <see cref="LaunchOutcome.MainScreens"/> and machine setup only from
    /// <see cref="LaunchOutcome.MachineSetup"/>, so no outcome shows a stage another outcome owns. Each stage takes
    /// the previous one's place in the same window, so there is never a second window and never a moment with
    /// none.
    /// </remarks>
    private static void ShowLaunchSurface(LaunchOutcome outcome)
    {
        switch (LaunchStageSelection.For(outcome))
        {
            case LaunchStage.Shell:
                HandOverToShell();
                break;

            case LaunchStage.SignIn:
                // The sign-in stage is where a launch ends when nobody is signed in yet (FR-001). It takes the
                // launch surface's place in the same window rather than opening one of its own.
                ShowLaunchStage(LaunchStage.SignIn, new SignInPage());
                break;

            case LaunchStage.MachineSetup:
                // Setup takes the launch surface's place for the same reason sign-in does: the window the person
                // is working in is the one that owns the ending when they abandon it (FR-008).
                ShowLaunchStage(LaunchStage.MachineSetup, new MachineSetupPage());
                break;

            case LaunchStage.Blocked:
                ShowBlockedStateStage();
                break;

            default:
                // The process is going, or an outcome no stage owns. There is no sixth outcome today (FR-001), so
                // this is the guard rather than a route, and it ends rather than leaving a launch running with
                // nothing to report to (SC-001).
                ExitApplication();
                break;
        }
    }

    /// <summary>
    /// Shows the stopped-launch stage and offers the fault to the repair policy before revealing it (FR-019).
    /// </summary>
    /// <remarks>
    /// The stage decides whether it is needed: a fault that can be put right without asking is put right and the
    /// launch carries on, so the stage stays collapsed and the host swaps the content when the launch reaches
    /// its next surface rather than a prompt appearing and taking itself away again (FR-019).
    /// </remarks>
    private static void ShowBlockedStateStage()
    {
        var page = new BlockedStatePage();

        ShowLaunchStage(LaunchStage.Blocked, page);

        _ = page.StartAsync();
    }

    /// <summary>
    /// Hands the launch over to the shell: the one window becomes the shell, maximised, and nothing else can
    /// reach it (`contracts/window-stage-contract.md` §§2-3; FR-019, FR-022, FR-043).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The shell is reachable only from the main-screens outcome.</b> This is the one place it is shown, and
    /// it is reached only from <see cref="LaunchOutcome.MainScreens"/>, which is what keeps a launch that did not
    /// reach the main screens away from it (FR-022, FR-043).
    /// </para>
    /// <para>
    /// <b>The shell is built here rather than in the host's constructor</b>, so it does not exist until the
    /// launch routes to it (FR-008). The one page is kept because the provider creates a new one per call: a
    /// second call would build a second shell and lose the person's place in the first. Without this the main
    /// screens are a blank window — which is what the store's log showed on 2026-09-28, and what the person saw.
    /// </para>
    /// </remarks>
    private static void HandOverToShell()
    {
        s_launchHandedOver = true;

        ShowShellContent();

        _launchStageSurface = null;

        MainWindow.ShowStage(LaunchStage.Shell, _shellContent!);
        MainWindow.Activate();
    }

    /// <summary>
    /// Puts the shell into the one window, once, the first time a launch reaches the main screens. The shell is
    /// maximized by the window itself as part of showing the shell stage, so a window that cannot be maximized
    /// still completes the hand-over at the size it opened at (FR-019).
    /// </summary>
    private static void ShowShellContent()
        => _shellContent ??= App.GetService<IShellContentProvider>().CreateShellContent();

    /// <summary>
    /// Answers the one window's close box while a launch stage is showing, by asking the stage what closing it
    /// means (FR-008).
    /// </summary>
    /// <remarks>
    /// The stage states its own reason — machine setup names the route it was dismissed by, a stop states its
    /// cause, the sign-in states that nobody signed in — rather than the host inventing one reason for all of
    /// them. The window's close was cancelled by the window itself, so it does not vanish before the reason has
    /// been stated.
    /// </remarks>
    internal static void OnMainWindowClosingDuringLaunch()
    {
        if (_launchStageSurface is { } surface)
        {
            _ = surface.RequestCloseAsync();

            return;
        }

        EndLaunch("Startup_Launch.EndedByWindowClose".GetLocalized());
    }

    /// <summary>
    /// States why the process is ending, then ends it through the launch, which is the only thing that may end it
    /// (FR-008).
    /// </summary>
    /// <param name="reason">Why the process is ending, in the reader's words.</param>
    private static void EndLaunch(string reason)
    {
        try
        {
            App.GetService<ILaunchPipeline>().End(reason);
        }
        catch (Exception exception)
        {
            // A launch that cannot be reached is still a process that was asked to end, so it ends with the
            // failure recorded the only way left to record it.
            AppLog.Error("Launch", exception, "The reason the process is ending could not be stated through the launch.");
            ExitApplication();
        }
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

    /// <summary>
    /// Applies the plant-wide log-store level to the threshold the logging seam keeps entries at.
    /// </summary>
    /// <param name="services">The host's services.</param>
    /// <remarks>
    /// <para>
    /// <b>Best effort and never awaited.</b> The shipped default is already in force, so the worst a store that
    /// will not answer can cost is that this machine keeps the default until the next launch — which must never
    /// be paid for with a launch that waits on diagnostics.
    /// </para>
    /// <para>
    /// <b>Nothing about it is user-gated.</b> Every computer signs in as somebody, every computer records what
    /// happens on it, and where the level comes from is a plant-wide row rather than a person's role. A host with
    /// no configuration store registered simply keeps the configured default.
    /// </para>
    /// </remarks>
    private static void AdoptStoredLogStoreLevel(IServiceProvider services)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (services.GetService<ILogStoreLevelService>() is not { } levels)
                {
                    return;
                }

                var applied = await levels.ApplyStoredLevelAsync().ConfigureAwait(false);

                AppLog.Info("LogStore", $"The log store keeps entries at '{applied}' and above.");
            }
            catch (Exception exception)
            {
                // The default is in force, so a stored level that could not be read is a diagnostic about the
                // diagnostics rather than a reason to do anything else about it.
                AppLog.Error("LogStore", exception, "The stored log-store level could not be applied, so the shipped default is in force.");
            }
        });
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
        //
        // <b>The exception's own StackTrace is empty here, so the live stack is the one that names the throw
        // site.</b> The runtime raises this event as the exception leaves the throw, before the exception has
        // captured a stack trace, so <c>e.Exception.StackTrace</c> and <c>ToString()</c> carry the type and the
        // message and nothing else — which is why the crashes of 2026-09-28 and 2026-10-01 left no usable record
        // even though this hook was installed. <see cref="Environment.StackTrace"/> is read here instead: it is
        // the stack the throw is standing on, so the record names the method and the line that threw.
        if (e.Exception is InvalidOperationException invalidOperationException)
        {
            var liveStack = Environment.StackTrace;

            if (liveStack.Contains("MTM_Waitlist", StringComparison.OrdinalIgnoreCase)
                || (invalidOperationException.StackTrace ?? string.Empty).Contains("MTM_Waitlist", StringComparison.OrdinalIgnoreCase))
            {
                AppLog.Error("FirstChance", invalidOperationException, "First-chance InvalidOperationException in MTM_Waitlist stack.");
                RecordFault(
                    "FirstChance",
                    $"{invalidOperationException}{Environment.NewLine}"
                    + $"--- the stack the throw was standing on ---{Environment.NewLine}{liveStack}");
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
