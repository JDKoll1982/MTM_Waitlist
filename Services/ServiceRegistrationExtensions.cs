using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

using MTM_Waitlist.Activation;
using MTM_Waitlist.Contracts.Services;
using MTM_Waitlist.Core.Contracts.Services;
using MTM_Waitlist.Core.Services;
using MTM_Waitlist.Models;
using MTM_Waitlist.Notifications;
using MTM_Waitlist.ViewModels;
using MTM_Waitlist.Views;

namespace MTM_Waitlist.Services;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services, HostBuilderContext context)
    {
        // Default activation handler
        services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

        // Other activation handlers
        services.AddTransient<IActivationHandler, AppNotificationActivationHandler>();

        // Services
        services.AddSingleton<IAppNotificationService, AppNotificationService>();
        services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
        services.AddSingleton<IStartupRecoveryService, StartupRecoveryService>();
        services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
        services.AddSingleton<IBuildingSelectionService, BuildingSelectionService>();
        services.AddSingleton<IStartupShellStateService, StartupShellStateService>();
        services.AddSingleton<IStartupCoordinator, StartupCoordinator>();
        services.AddSingleton<StartupState>();
        services.AddTransient<INavigationViewService, NavigationViewService>();
        services.AddSingleton<IActivationService, ActivationService>();
        services.AddSingleton<IPageService, PageService>();
        services.AddSingleton<INavigationService, NavigationService>();

        // Core services
        services.AddSingleton<ISampleDataService, SampleDataService>();
        services.AddSingleton<IFileService, FileService>();

        // Views and view models
        services.AddTransient<SplashViewModel>();
        services.AddTransient<SplashPage>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<DeveloperModeViewModel>();
        services.AddTransient<DeveloperModePage>();
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
        services.Configure<StartupWindowOptions>(context.Configuration.GetSection(nameof(StartupWindowOptions)));

        return services;
    }
}
