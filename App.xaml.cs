using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
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
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers
            services.AddTransient<IActivationHandler, AppNotificationActivationHandler>();

            // Services
            services.AddSingleton<IAppNotificationService, AppNotificationService>();
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddTransient<INavigationViewService, NavigationViewService>();
            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Core Services
            services.AddSingleton<ISampleDataService, SampleDataService>();
            services.AddSingleton<IFileService, FileService>();

            // Views and ViewModels
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsPage>();
            services.AddTransient<WaitlistViewDetailViewModel>();
            services.AddTransient<WaitlistViewDetailPage>();
            services.AddTransient<WaitlistViewViewModel>();
            services.AddTransient<WaitlistViewPage>();
            services.AddTransient<MainShellViewModel>();
            services.AddTransient<MainShellPage>();
            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();

            // Configuration
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
        }).
        Build();

        _ = MainWindow;
        App.GetService<IAppNotificationService>().Initialize();
        UnhandledException += App_UnhandledException;

        // FIX 1: Listen to Window closure to trigger container host termination
        MainWindow.Closed += MainWindow_Closed;
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        MainWindow.Closed -= MainWindow_Closed;

        _ = ShutdownAsync();
    }

    private async Task ShutdownAsync()
    {
        try
        {
            App.GetService<IAppNotificationService>().Unregister();
        }
        catch { }

        try
        {
            await Host.StopAsync().ConfigureAwait(false);
        }
        catch { }
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // Log and handle exceptions as appropriate.
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        // Starts the generic background thread host runtime manager engine
        await Host.StartAsync();

        App.GetService<IAppNotificationService>().Show(string.Format("AppNotificationSamplePayload".GetLocalized(), AppContext.BaseDirectory));
        await App.GetService<IActivationService>().ActivateAsync(args);
    }
}
