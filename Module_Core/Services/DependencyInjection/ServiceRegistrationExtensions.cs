using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

using MTM_Waitlist.Activation;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Core.Services.DependencyInjection;
using MTM_Waitlist.Module_DevTools.ViewModels;
using MTM_Waitlist.Module_DevTools.Views;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Module_Settings.Views;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.ViewModels;
using MTM_Waitlist.Module_Startup.Views;
using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Module_Waitlist.Views;
using MTM_Waitlist.Module_Core.ViewModels;
using MTM_Waitlist.Module_Core.Views;
using MTM_Waitlist.Notifications;

namespace MTM_Waitlist.Module_Core.Services.DependencyInjection;

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
        services.AddSingleton<ILocalSettingsService, MTM_Waitlist.Module_Settings.Services.LocalSettingsService>();
        services.AddSingleton<IStartupRecoveryService, MTM_Waitlist.Module_Startup.Services.StartupRecoveryService>();
        services.AddSingleton<IStartupRegistrationService, MTM_Waitlist.Module_Startup.Services.StartupRegistrationService>();
        services.AddSingleton<IStartupSessionRepository, MTM_Waitlist.Module_Startup.Services.StartupSessionRepository>();
        services.AddSingleton<IStartupLogForwarder, MTM_Waitlist.Module_Startup.Services.StartupLogForwarder>();
        services.AddSingleton<MTM_Waitlist.Module_Startup.Services.StartupLogService>();
        services.AddSingleton<IStartupLogService>(provider => provider.GetRequiredService<MTM_Waitlist.Module_Startup.Services.StartupLogService>());
        services.AddSingleton<IHostedService>(provider => provider.GetRequiredService<MTM_Waitlist.Module_Startup.Services.StartupLogService>());
        services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
        services.AddSingleton<IBuildingSelectionService, BuildingSelectionService>();
        services.AddSingleton<IStartupShellStateService, MTM_Waitlist.Module_Startup.Services.StartupShellStateService>();
        services.AddSingleton<IStartupCoordinator, MTM_Waitlist.Module_Startup.Services.StartupCoordinator>();
        services.AddSingleton<MTM_Waitlist.Module_Startup.Models.StartupState>();
        services.AddTransient<INavigationViewService, NavigationViewService>();
        services.AddSingleton<IActivationService, ActivationService>();
        services.AddSingleton<IPageService, PageService>();
        services.AddSingleton<INavigationService, NavigationService>();

        // Core services
        services.AddSingleton<ISampleDataService, SampleDataService>();
        services.AddSingleton<IFileService, FileService>();

        // Views and view models
        services.AddTransient<SplashViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<LoginPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<DeveloperModeViewModel>();
        services.AddTransient<DeveloperModePage>();
        services.AddTransient<WaitlistViewDetailViewModel>();
        services.AddTransient<WaitlistViewDetailPage>();
        services.AddTransient<WaitlistViewViewModel>();
        services.AddTransient<WaitlistViewPage>();
        services.AddTransient<RequestTypeBuilderViewModel>();
        services.AddTransient<RequestTypeBuilderPage>();
        services.AddTransient<ShellPage>();
        services.AddTransient<ShellViewModel>();

        // Configuration
        services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
        services.Configure<StartupDatabaseOptions>(context.Configuration.GetSection(nameof(StartupDatabaseOptions)));
        services.Configure<StartupDevelopmentOptions>(context.Configuration.GetSection(nameof(StartupDevelopmentOptions)));
        services.Configure<StartupLoggingOptions>(context.Configuration.GetSection(nameof(StartupLoggingOptions)));
        services.Configure<StartupWindowOptions>(context.Configuration.GetSection(nameof(StartupWindowOptions)));

        services.AddModuleServices(context.Configuration);

        return services;
    }
}
