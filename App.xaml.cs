using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using MTM_Waitlist.Activation;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Startup.Views;
using MTM_Waitlist.Notifications;
using MTM_Waitlist.Services.DependencyInjection;
#if DEBUG
using XamlMcp.WinUI;
#endif

namespace MTM_Waitlist;

public partial class App : Application
{
    private static WindowEx? _mainWindow;
    private static SplashWindow? _splashWindow;
    private static LoginWindow? _loginWindow;
    private static bool _mainWindowActivated;
    private static bool _loginWindowActivated;
#if DEBUG
    private static WinUiXamlMcpSession? _xamlMcpSession;
#endif

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

    public App()
    {
        StartupDebugLog.Info("App", "App constructor started.");
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) => services.AddAppServices(context)).
        Build();

        AppServiceLocator.NavigationService = Host.Services.GetService<INavigationService>();
        SharedServiceLocator.TooltipService = Host.Services.GetService<ITooltipService>();
        SharedServiceLocator.ControlInspectorService = Host.Services.GetService<IControlInspectorService>();

        StartupDebugLog.Configure(Host.Services.GetService<IStartupLogService>());
        StartupDebugLog.Info("App", "Host built.");

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

    public static void ShowSplashWindow()
    {
        StartupDebugLog.Info("Splash", "ShowSplashWindow called.");

        if (_splashWindow == null)
        {
            _splashWindow = new SplashWindow();
            _splashWindow.Closed += SplashWindow_Closed;
            StartupDebugLog.Info("Splash", "Splash window created.");
        }

        _splashWindow.Activate();
#if DEBUG
        RegisterXamlMcpWindow(_splashWindow);
#endif
        StartupDebugLog.Info("Splash", "Splash window activated.");
    }

    public static void ShowMainWindowAndCloseSplash()
    {
        StartupDebugLog.Info("MainWindow", "Activating main window and closing splash.");
        try
        {
            MainWindow.Closed += MainWindow_Closed;
            _mainWindowActivated = true;
            MainWindow.Activate();
#if DEBUG
            RegisterXamlMcpWindow(MainWindow);
#endif
            CloseSplashWindow();
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("MainWindow", ex, "Failed while activating main window or closing splash.");
            Current.Exit();
        }
    }

    public static void ShowLoginWindowAndCloseSplash()
    {
        StartupDebugLog.Info("LoginWindow", "Activating login window and closing splash.");

        try
        {
            if (_loginWindow == null)
            {
                _loginWindow = App.GetService<LoginWindow>();
                _loginWindow.Closed += LoginWindow_Closed;
                StartupDebugLog.Info("LoginWindow", "Login window created.");
            }

            _loginWindowActivated = true;
            _loginWindow.Activate();
#if DEBUG
            RegisterXamlMcpWindow(_loginWindow);
#endif
            CloseSplashWindow();
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("LoginWindow", ex, "Failed while activating login window or closing splash.");
            Current.Exit();
        }
    }

    public static void ShowMainWindowAndCloseLoginWindow()
    {
        StartupDebugLog.Info("MainWindow", "Activating main window and closing login window.");

        try
        {
            MainWindow.Closed += MainWindow_Closed;
            _mainWindowActivated = true;
            MainWindow.Activate();
#if DEBUG
            RegisterXamlMcpWindow(MainWindow);
#endif

            if (_loginWindow is not null)
            {
                var windowToClose = _loginWindow;
                _loginWindow = null;
                _loginWindowActivated = false;
                windowToClose.Closed -= LoginWindow_Closed;
                windowToClose.Close();
            }
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("MainWindow", ex, "Failed while activating main window or closing login window.");
            Current.Exit();
        }
    }

#if DEBUG
    private static void AttachXamlMcpAgent()
    {
        if (_xamlMcpSession is not null)
        {
            return;
        }

        try
        {
            _xamlMcpSession = WinUiXamlMcp.Attach();
            StartupDebugLog.Info("XamlMcp", "WinUI inspector agent attached (debug diagnostics).");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("XamlMcp", ex, "Failed to attach the WinUI inspector agent.");
        }
    }

    private static void RegisterXamlMcpWindow(Window window)
    {
        if (_xamlMcpSession is null || window is null)
        {
            return;
        }

        try
        {
            _ = _xamlMcpSession.RegisterWindow(window);
            StartupDebugLog.Info("XamlMcp", $"Registered '{window.GetType().Name}' for WinUI inspection.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("XamlMcp", ex, $"Failed to register '{window.GetType().Name}' for WinUI inspection.");
        }
    }
#endif

    private static void CloseSplashWindow()
    {
        if (_splashWindow == null)
        {
            return;
        }

        var windowToClose = _splashWindow;
        _splashWindow = null;
        windowToClose.Closed -= SplashWindow_Closed;
        windowToClose.Close();
        StartupDebugLog.Info("Splash", "Splash window closed.");
    }

    private static void SplashWindow_Closed(object sender, WindowEventArgs args)
    {
        StartupDebugLog.Info("Splash", $"Splash window closed event. MainWindowActivated={_mainWindowActivated}, LoginWindowActivated={_loginWindowActivated}.");
        if (!_mainWindowActivated && !_loginWindowActivated)
        {
            Current.Exit();
        }
    }

    private static void LoginWindow_Closed(object sender, WindowEventArgs args)
    {
        StartupDebugLog.Info("LoginWindow", "Login window closed event received.");

        if (_loginWindow is null)
        {
            return;
        }

        _loginWindow.Closed -= LoginWindow_Closed;
        _loginWindow = null;
        _loginWindowActivated = false;

        if (!_mainWindowActivated)
        {
            Current.Exit();
        }
    }

    private static void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        StartupDebugLog.Info("MainWindow", "Main window closed event received.");
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.Closed -= MainWindow_Closed;

        if (Current is App app)
        {
            _ = app.ShutdownAsync();
        }
    }

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

#if DEBUG
        if (_xamlMcpSession is { } xamlMcpSession)
        {
            _xamlMcpSession = null;
            try
            {
                await xamlMcpSession.DisposeAsync().ConfigureAwait(false);
                StartupDebugLog.Info("XamlMcp", "WinUI inspector session disposed.");
            }
            catch (Exception ex)
            {
                StartupDebugLog.Error("XamlMcp", ex, "Failed to dispose the WinUI inspector session during shutdown.");
            }
        }
#endif
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

#if DEBUG
        AttachXamlMcpAgent();
#endif

        try
        {
            // Starts the generic background thread host runtime manager engine
            await Host.StartAsync();
            StartupDebugLog.Info("Launch", "Host started.");

            App.GetService<IAppNotificationService>().Show(string.Format("AppNotificationSamplePayload".GetLocalized(), AppContext.BaseDirectory));
            StartupDebugLog.Info("Launch", "App notification shown.");

            await App.GetService<IActivationService>().ActivateAsync(args, activateMainWindow: false);
            StartupDebugLog.Info("Launch", "Activation service completed with deferred main window activation.");

            ShowSplashWindow();
            StartupDebugLog.Info("Launch", "Splash requested from OnLaunched.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("Launch", ex, "Unhandled exception during launch pipeline.");
            throw;
        }
    }
}
