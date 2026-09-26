using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Startup.Views;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Notifications;
using MTM_Waitlist.Services.DependencyInjection;

namespace MTM_Waitlist;

public partial class App : Application
{
    private static Microsoft.UI.Dispatching.DispatcherQueue? _uiDispatcher;
    private static WindowEx? _mainWindow;
    private static StartupPlaceholderWindow? _placeholderWindow;

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
        StartupDebugLog.Info("App", "App constructor started.");

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

        StartupDebugLog.Configure(Host.Services.GetService<IStartupLogService>());
        StartupDebugLog.Info("App", "Host built.");

        try
        {
            // The probe driver is what lets the read state leave Unknown and the shell indicator appear
            // (T123). Probing only: refresh scheduling belongs to the on-host service (FR-025), and a probe
            // failure must never block startup.
            App.GetService<IVisualReachabilityProbeHost>().Start();
            StartupDebugLog.Info("App", "Visual reachability probe host started.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("App", ex, "The Visual reachability probe host failed to start.");
        }

        try
        {
            App.GetService<IAppNotificationService>().Initialize();
            StartupDebugLog.Info("App", "App notification service initialized.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("App", ex, "App notification service failed to initialize.");
            throw;
        }

        UnhandledException += App_UnhandledException;
        StartupDebugLog.Info("App", "UnhandledException handler registered.");

    #if DEBUG
        AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
    #endif
    }

    public static void ShowStartupPlaceholderWindow()
    {
        StartupDebugLog.Info("StartupPlaceholder", "ShowStartupPlaceholderWindow called.");
        _placeholderWindow ??= new StartupPlaceholderWindow();
        _placeholderWindow.Activate();
        StartupDebugLog.Info("StartupPlaceholder", "Startup placeholder window activated.");
    }

    /// <summary>
    /// Stops the host so no background service outlives the last window.
    /// </summary>
    public static Task ShutdownHostAsync() =>
        (Current as App)?.ShutdownAsync() ?? Task.CompletedTask;

    private async Task ShutdownAsync()
    {
        StartupDebugLog.Info("Shutdown", "Shutdown started.");
        try
        {
            App.GetService<IAppNotificationService>().Unregister();
            StartupDebugLog.Info("Shutdown", "App notification service unregistered.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("Shutdown", ex, "Failed to unregister app notification service.");
        }

        try
        {
            await Host.StopAsync().ConfigureAwait(false);
            StartupDebugLog.Info("Shutdown", "Host stopped.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("Shutdown", ex, "Failed to stop host.");
        }
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        if (e.Exception is not null)
        {
            StartupDebugLog.Error("UnhandledException", e.Exception, $"Unhandled exception message: {e.Message}");
            return;
        }

        StartupDebugLog.Info("UnhandledException", $"Unhandled exception message: {e.Message}");
    }

    private static void CurrentDomain_FirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
    {
#if DEBUG
        if (e.Exception is COMException comException)
        {
            var stack = comException.StackTrace ?? string.Empty;
            if (stack.Contains("MTM_Waitlist", StringComparison.OrdinalIgnoreCase))
            {
                StartupDebugLog.Error("FirstChance", comException, $"First-chance COMException in MTM_Waitlist stack. HResult=0x{comException.HResult:X8}.");
            }
        }

        if (e.Exception is NullReferenceException nullReferenceException)
        {
            var stack = nullReferenceException.StackTrace ?? string.Empty;
            if (stack.Contains("MTM_Waitlist", StringComparison.OrdinalIgnoreCase))
            {
                StartupDebugLog.Error("FirstChance", nullReferenceException, "First-chance NullReferenceException in MTM_Waitlist stack.");
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
                StartupDebugLog.Error(
                    "FirstChance",
                    fileNotFoundException,
                    $"First-chance FileNotFoundException. FileName='{fileName}'.");
            }
        }
#endif
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        StartupDebugLog.Info("Launch", "OnLaunched started.");
        base.OnLaunched(args);

        try
        {
            // Starts the generic background thread host runtime manager engine
            await Host.StartAsync();
            StartupDebugLog.Info("Launch", "Host started.");

            // The placeholder is the only surface a launch produces while the startup pipeline is rebuilt. No
            // shell navigation, no sign-in form and no store read happens on this path.
            ShowStartupPlaceholderWindow();
            StartupDebugLog.Info("Launch", "Startup placeholder requested from OnLaunched.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("Launch", ex, "Unhandled exception during launch pipeline.");
            throw;
        }
    }
}
