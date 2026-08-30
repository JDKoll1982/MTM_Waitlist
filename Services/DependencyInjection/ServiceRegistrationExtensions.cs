using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

using MTM_Waitlist.Activation;
using MTM_Waitlist.Services;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Module_Settings.Views;
using MTM_Waitlist.Module_Setup.ViewModels;
using MTM_Waitlist.Module_Setup.Views;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Startup.ViewModels;
using MTM_Waitlist.Module_Startup.Views;
using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Module_Waitlist.Views;
using MTM_Waitlist.Module_Core.Views;
using MTM_Waitlist.ViewModels;
using MTM_Waitlist.Module_Shared.ViewModels;
using MTM_Waitlist.Module_Shared.Views;
using MTM_Waitlist.Notifications;

namespace MTM_Waitlist.Services.DependencyInjection;

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
        services.AddSingleton<IAppWindowProvider, AppWindowProvider>();
        services.AddSingleton<IShellContentProvider, ShellContentProvider>();
        services.AddSingleton<ILocalSettingsService, MTM_Waitlist.Module_Settings.Services.LocalSettingsService>();
        services.AddSingleton<IStartupRecoveryService, MTM_Waitlist.Module_Startup.Services.StartupRecoveryService>();
        services.AddSingleton<IStartupRegistrationService, MTM_Waitlist.Module_Startup.Services.StartupRegistrationService>();
        services.AddSingleton<IStartupSessionRepository, MTM_Waitlist.Module_Startup.Services.StartupSessionRepository>();
        services.AddSingleton<IComputerRegistryService, MTM_Waitlist.Module_Startup.Services.ComputerRegistryService>();
        services.AddSingleton<IComputerGateService, MTM_Waitlist.Module_Startup.Services.ComputerGateService>();
        services.AddSingleton<IStartupWindowService, MTM_Waitlist.Module_Startup.Services.StartupWindowService>();
        services.AddSingleton<IStartupLogForwarder, MTM_Waitlist.Module_Startup.Services.StartupLogForwarder>();
        services.AddSingleton<MTM_Waitlist.Module_Startup.Services.StartupLogService>();
        services.AddSingleton<IStartupLogService>(provider => provider.GetRequiredService<MTM_Waitlist.Module_Startup.Services.StartupLogService>());
        services.AddSingleton<IHostedService>(provider => provider.GetRequiredService<MTM_Waitlist.Module_Startup.Services.StartupLogService>());
        services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
        services.AddSingleton<IBuildingSelectionService, BuildingSelectionService>();
        services.AddSingleton<IStartupShellStateService, MTM_Waitlist.Module_Startup.Services.StartupShellStateService>();
        services.AddSingleton<IStartupCoordinator, MTM_Waitlist.Module_Startup.Services.StartupCoordinator>();
        services.AddSingleton<MTM_Waitlist.Module_Core.Models.StartupState>();
        services.AddTransient<INavigationViewService, NavigationViewService>();
        services.AddSingleton<IActivationService, ActivationService>();
        services.AddSingleton<IPageService, PageService>();
        services.AddSingleton<IPageTransitionService, PageTransitionService>();
        services.AddSingleton<INavigationService, NavigationService>();

        // Core services
        services.AddSingleton<ISampleDataService>(serviceProvider =>
            new SampleDataService(serviceProvider.GetRequiredService<ILocalSettingsService>()));
        services.AddSingleton<SqlHelperServer>();
        services.AddSingleton<MySqlHelperServer>();
        services.AddSingleton<MTM_Waitlist.Module_Core.Contracts.Services.IMySqlHelperServer>(
            sp => sp.GetRequiredService<MySqlHelperServer>());
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IReportPrintService, ReportPrintService>();

        // Views and view models
        services.AddTransient<SplashViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<LoginWindow>();
        services.AddTransient<LoginPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<SetupWorkOrderViewModel>();
        services.AddTransient<SetupWorkCenterViewModel>();
        services.AddTransient<SetupPartSelectionViewModel>();
        services.AddTransient<SetupSequenceSelectionViewModel>();
        services.AddTransient<SetupDunnageTypeViewModel>();
        services.AddTransient<SetupReviewViewModel>();
        services.AddTransient<SetupCompletionViewModel>();
        services.AddTransient<SetupDunnageImageSearchDialogViewModel>();
        services.AddTransient<SetupDunnageImageSearchDialog>();
        services.AddTransient<SetupWorkOrderPage>();
        services.AddTransient<SetupWorkCenterPage>();
        services.AddTransient<SetupPartSelectionPage>();
        services.AddTransient<SetupSequenceSelectionPage>();
        services.AddTransient<SetupDunnageTypePage>();
        services.AddTransient<SetupReviewPage>();
        services.AddTransient<SetupCompletionPage>();
        services.AddTransient<WaitlistViewDetailViewModel>();
        services.AddTransient<WaitlistViewDetailPage>();
        services.AddTransient<NewRequestWorkCenterViewModel>();
        services.AddTransient<NewRequestWorkCenterPage>();
        services.AddTransient<NewRequestJobTypeViewModel>();
        services.AddTransient<NewRequestJobTypePage>();
        services.AddTransient<NewRequestSubtypeViewModel>();
        services.AddTransient<NewRequestSubtypePage>();
        services.AddTransient<NewRequestDetailsViewModel>();
        services.AddTransient<NewRequestDetailsPage>();
        services.AddTransient<NewRequestPreviewViewModel>();
        services.AddTransient<NewRequestPreviewPage>();
        services.AddTransient<NewRequestSummaryViewModel>();
        services.AddTransient<NewRequestSummaryPage>();
        services.AddTransient<NewRequestResultViewModel>();
        services.AddTransient<NewRequestResultPage>();
        services.AddTransient<ControlInspectorDetailViewModel>();
        services.AddTransient<ControlInspectorDetailPage>();
        services.AddTransient<WaitlistViewViewModel>(provider => new WaitlistViewViewModel(
            provider.GetRequiredService<INavigationService>(),
            provider.GetRequiredService<ISampleDataService>(),
            provider.GetRequiredService<IBuildingSelectionService>(),
            provider.GetRequiredService<MTM_Waitlist.Module_Waitlist.Services.IWaitlistRequestService>(),
            provider.GetRequiredService<MTM_Waitlist.Module_Settings.Services.IImageLocationService>(),
            DispatcherQueue.GetForCurrentThread()));
        services.AddTransient<WaitlistViewPage>();
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
