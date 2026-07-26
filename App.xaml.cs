using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System.Runtime.ExceptionServices;
using MTM_Waitlist.Activation;
using MTM_Waitlist.Contracts.Services;
using MTM_Waitlist.Core.Contracts.Services;
using MTM_Waitlist.Core.Services;
using MTM_Waitlist.Helpers;
using MTM_Waitlist.Models;
using MTM_Waitlist.Notifications;
using MTM_Waitlist.Services;
using MTM_Waitlist.ViewModels;
using MTM_Waitlist.Views;

namespace MTM_Waitlist;

public partial class App : Application
{
    private static WindowEx? _mainWindow;
    private static SplashWindow? _splashWindow;
    private static bool _mainWindowActivated;

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
            CloseSplashWindow();
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("MainWindow", ex, "Failed while activating main window or closing splash.");
            Current.Exit();
        }
    }

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
        StartupDebugLog.Info("Splash", $"Splash window closed event. MainWindowActivated={_mainWindowActivated}.");
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
        if (e.Exception is NullReferenceException nullReferenceException)
        {
            var stack = nullReferenceException.StackTrace ?? string.Empty;
            if (stack.Contains("MTM_Waitlist", StringComparison.OrdinalIgnoreCase))
            {
                StartupDebugLog.Error("FirstChance", nullReferenceException, "First-chance NullReferenceException in MTM_Waitlist stack.");
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
