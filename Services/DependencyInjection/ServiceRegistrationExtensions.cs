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
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Services;
using MTM_Waitlist.Module_Setup.ViewModels;
using MTM_Waitlist.Module_Setup.Views;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Startup.ViewModels;
using MTM_Waitlist.Module_Startup.Views;
using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Module_Waitlist.Views;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Core.Views;
using MTM_Waitlist.ViewModels;
using MTM_Waitlist.Module_Shared.ViewModels;
using MTM_Waitlist.Module_Shared.Views;
using MTM_Waitlist.Mock.DependencyInjection;
using MTM_Waitlist.Notifications;

namespace MTM_Waitlist.Services.DependencyInjection;

public static partial class ServiceRegistrationExtensions
{
    /// <summary>
    /// The routes feature 006's story phases contribute. Each phase registers its own page in its own file and adds
    /// its route here, and the page service applies them as it builds its route table, so no phase edits the
    /// factory the others also edit.
    /// </summary>
    private static readonly List<Action<PageService>> s_pageRouteRegistrations = [];

    public static IServiceCollection AddAppServices(this IServiceCollection services, HostBuilderContext context)
    {
        // Default activation handler
        services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

        // Other activation handlers
        services.AddTransient<IActivationHandler, AppNotificationActivationHandler>();

        // Services
        services.AddSingleton<IAppNotificationService, AppNotificationService>();
        services.AddSingleton<IAppWindowProvider, AppWindowProvider>();
        services.AddSingleton<IDeepLinkWindow, AppWindowDeepLinkWindow>();
        services.AddSingleton<RequestDeepLinkHandler>();
        services.AddSingleton<IShellContentProvider, ShellContentProvider>();
        services.AddSingleton<IAppLifecycleService, AppLifecycleService>();
        services.AddSingleton<ISetupDialogService, SetupDialogService>();
        // The New Request dunnage step's substitute picker reuses the Setup dunnage image-search dialog, so the
        // bridge lives here where both sides are visible (FR-049).
        services.AddSingleton<MTM_Waitlist.Module_Settings.Services.IDunnageSubstitutePicker, DunnageSubstitutePicker>();
        services.AddSingleton<IWaitlistRequestActionPrompt, WaitlistRequestActionPrompt>();
        services.AddSingleton<IWorkCenterImageService, MTM_Waitlist.Module_Settings.Services.ImageLocationService>();
        services.AddSingleton<ILocalSettingsService, MTM_Waitlist.Module_Settings.Services.LocalSettingsService>();
        services.AddSingleton<IIgnoredLocationsService, MTM_Waitlist.Module_Core.Services.IgnoredLocationsService>();
        services.AddSingleton<IStartupRecoveryService, MTM_Waitlist.Module_Startup.Services.StartupRecoveryService>();
        services.AddSingleton<IAppProcessRestarter, MTM_Waitlist.Module_Startup.Services.AppProcessRestarter>();
        services.AddSingleton<ISignOutService, MTM_Waitlist.Module_Startup.Services.SignOutService>();
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
        services.AddSingleton<PageService>();
        services.AddSingleton<IPageService>(sp =>
        {
            var pageService = sp.GetRequiredService<PageService>();
            pageService.Configure<WaitlistViewViewModel, WaitlistViewPage>();
            pageService.Configure<WaitlistViewDetailViewModel, WaitlistViewDetailPage>();
            pageService.Configure<NewRequestWorkCenterViewModel, NewRequestWorkCenterPage>();
            pageService.Configure<NewRequestJobTypeViewModel, NewRequestJobTypePage>();
            pageService.Configure<NewRequestItemViewModel, NewRequestItemPage>();
            pageService.Configure<NewRequestDunnageViewModel, NewRequestDunnagePage>();
            pageService.Configure<NewRequestDieViewModel, NewRequestDiePage>();
            pageService.Configure<NewRequestComponentViewModel, NewRequestComponentPage>();
            pageService.Configure<NewRequestDetailsViewModel, NewRequestDetailsPage>();
            pageService.Configure<NewRequestSummaryViewModel, NewRequestSummaryPage>();
            pageService.Configure<NewRequestResultViewModel, NewRequestResultPage>();
            pageService.Configure<ControlInspectorDetailViewModel, ControlInspectorDetailPage>();
            pageService.Configure<SettingsViewModel, SettingsPage>();
            pageService.Configure<SetupWorkCenterViewModel, SetupWorkCenterPage>();
            pageService.Configure<SetupWorkOrderViewModel, SetupWorkOrderPage>();
            pageService.Configure<SetupPartSelectionViewModel, SetupPartSelectionPage>();
            pageService.Configure<SetupSequenceSelectionViewModel, SetupSequenceSelectionPage>();
            pageService.Configure<SetupDunnageTypeViewModel, SetupDunnageTypePage>();
            pageService.Configure<SetupReviewViewModel, SetupReviewPage>();
            pageService.Configure<SetupCompletionViewModel, SetupCompletionPage>();

            // Feature 006's story phases each own their own registration file, so the routes they add are
            // contributed there and applied here rather than being written into this factory by three phases.
            foreach (var configureRoutes in s_pageRouteRegistrations)
            {
                configureRoutes(pageService);
            }

            return pageService;
        });
        services.AddSingleton<IPageTransitionService, PageTransitionService>();
        services.AddSingleton<INavigationService, NavigationService>();

        // Core services. The app's own store, the floor/WIP store, and the receiving store are always read
        // and written live: no sample/demo catalog and no mock short-circuit is registered (FR-001, FR-014).
        services.AddSingleton<MTM_Waitlist.Module_Core.Services.InforVisualSqlQueryService>();

        // Internal-store availability (FR-021): the seam records the outcome of every bounded-retry read here,
        // and a screen reads it to show its own Unavailable state with a manual retry.
        services.AddSingleton<MTM_Waitlist.Module_Core.Contracts.Services.IStoreAvailabilityTracker,
            MTM_Waitlist.Module_Core.Services.StoreAvailabilityTracker>();
        services.AddSingleton(MTM_Waitlist.Module_Core.Services.InternalStoreRetryPolicy.Default);
        services.AddSingleton<MySqlHelperServer>();
        services.AddSingleton<MTM_Waitlist.Module_Core.Contracts.Services.IMySqlHelperServer>(
            sp => sp.GetRequiredService<MySqlHelperServer>());

        // The account records, read by the employee identifier the signed-in session carries. The request flow
        // attributes a request from this lookup rather than from a name or a literal (FR-046).
        services.AddSingleton<
            MTM_Waitlist.Module_Core.Contracts.Services.IEmployeeDirectoryService,
            MTM_Waitlist.Module_Waitlist.Services.EmployeeDirectoryService>();

        // Infor Visual read fallback: the reachability detector and the five shape fallbacks, so every
        // Visual read is attempted live and transparently falls back to the mtm_mock mirror only when
        // the source is unreachable (FR-002, FR-004).
        services.AddVisualReadFallback();

        services.AddSingleton<MTM_Waitlist.Module_Core.Services.WipFloorInventoryService>();
        services.AddSingleton<MTM_Waitlist.Module_Settings.Services.RequestDispositionResolver>();
        services.AddSingleton<MTM_Waitlist.Module_Settings.Services.IRequestItemCatalogService, MTM_Waitlist.Module_Settings.Services.RequestItemCatalogService>();
        services.AddSingleton<MTM_Waitlist.Module_Settings.Services.INewRequestPickerService, MTM_Waitlist.Module_Settings.Services.NewRequestPickerService>();
        services.AddSingleton<MTM_Waitlist.Module_Settings.Services.IRequestItemConfigurationService, MTM_Waitlist.Module_Settings.Services.RequestItemConfigurationService>();
        services.AddSingleton<MTM_Waitlist.Module_Settings.Services.RequestItemLine2Resolver>();
        services.AddSingleton<MTM_Waitlist.Module_Settings.Services.IRequestItemObservedTimeService, MTM_Waitlist.Module_Settings.Services.RequestItemObservedTimeService>();

        // The availability snapshot's mapping is owned here, because the composition root is the only place
        // that sees both the Setup resolver and the Settings snapshot. It is *invoked* when the Item step is
        // entered, never at registration time (contract §3).
        services.AddSingleton<MTM_Waitlist.Module_Settings.Services.IRequestJobPartAvailabilityProvider, RequestJobAvailabilityProvider>();
        services.AddSingleton<MTM_Waitlist.Module_Settings.Services.IDefectTypeCatalogService, MTM_Waitlist.Module_Settings.Services.DefectTypeCatalogService>();
        services.AddSingleton<IExternalConnectionInfoProvider, ExternalConnectionInfoProvider>();
        services.AddSingleton<IConnectionHealthService, MTM_Waitlist.Module_Core.Services.ConnectionHealthService>();
        services.AddSingleton<INewRequestAlertService, MTM_Waitlist.Module_Core.Services.NewRequestAlertService>();
        services.AddSingleton<INewRequestAlertNotifier, MTM_Waitlist.Module_Core.Services.NewRequestAlertNotifier>();
        services.AddSingleton<IUrgencySettingsService, MTM_Waitlist.Module_Core.Services.UrgencySettingsService>();
        services.AddSingleton<IUrgencyDeadlineService, MTM_Waitlist.Module_Core.Services.UrgencyDeadlineService>();

        // The list's order is a per-viewer display preference remembered through the existing local settings,
        // not a new store (§D7, FR-011).
        services.AddSingleton<IWaitlistSortPreferenceService, MTM_Waitlist.Module_Core.Services.WaitlistSortPreferenceService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IReportPrintService, ReportPrintService>();

        // Views and view models
        services.AddTransient<SplashViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<UrgencyAllotmentEditorViewModel>();
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
        services.AddTransient<WaitlistViewDetailViewModel>(provider => new WaitlistViewDetailViewModel(
            navigationService: provider.GetRequiredService<INavigationService>(),
            buildingSelectionService: provider.GetRequiredService<IBuildingSelectionService>(),
            imageLocationService: provider.GetRequiredService<MTM_Waitlist.Module_Settings.Services.IImageLocationService>(),
            requestService: provider.GetRequiredService<MTM_Waitlist.Module_Waitlist.Services.IWaitlistRequestService>(),
            inventoryService: provider.GetRequiredService<MTM_Waitlist.Module_Waitlist.Services.IWaitlistInventoryService>(),
            startupState: provider.GetRequiredService<MTM_Waitlist.Module_Core.Models.StartupState>(),
            dispatcherQueue: DispatcherQueue.GetForCurrentThread(),
            messageSeenStore: provider.GetRequiredService<MTM_Waitlist.Module_Waitlist.Services.IWaitlistMessageSeenStore>(),
            itemConfigurationService: provider.GetRequiredService<MTM_Waitlist.Module_Settings.Services.IRequestItemConfigurationService>(),
            jobAvailabilityProvider: provider.GetRequiredService<MTM_Waitlist.Module_Settings.Services.IRequestJobPartAvailabilityProvider>(),
            partPictureResolver: provider.GetRequiredService<MTM_Waitlist.Module_Core.Contracts.Services.IPartPictureResolver>()));
        services.AddTransient<WaitlistViewDetailPage>();
        services.AddTransient<NewRequestWorkCenterViewModel>();
        services.AddTransient<NewRequestWorkCenterPage>();
        services.AddTransient<NewRequestJobTypeViewModel>();
        services.AddTransient<NewRequestJobTypePage>();
        services.AddTransient<NewRequestItemViewModel>();
        services.AddTransient<NewRequestItemPage>();
        services.AddTransient<NewRequestDunnageViewModel>();
        services.AddTransient<NewRequestDunnagePage>();
        services.AddTransient<NewRequestDieViewModel>();
        services.AddTransient<NewRequestDiePage>();
        services.AddTransient<NewRequestComponentViewModel>();
        services.AddTransient<NewRequestComponentPage>();
        services.AddTransient<NewRequestDetailsViewModel>();
        services.AddTransient<NewRequestDetailsPage>();
        services.AddTransient<NewRequestSummaryViewModel>();
        services.AddTransient<NewRequestSummaryPage>();
        services.AddTransient<NewRequestResultViewModel>();
        services.AddTransient<NewRequestResultPage>();
        services.AddTransient<ControlInspectorDetailViewModel>();
        services.AddTransient<ControlInspectorDetailPage>();
        // Named arguments on purpose: every constructor parameter is supplied explicitly. A positional call
        // would silently leave the optional ones null, and a null prompt seam means the handler gets no
        // confirmation and no lost-claim warning at all — a control the user is shown whose wiring is absent.
        services.AddTransient<WaitlistViewViewModel>(provider => new WaitlistViewViewModel(
            navigationService: provider.GetRequiredService<INavigationService>(),
            buildingSelectionService: provider.GetRequiredService<IBuildingSelectionService>(),
            waitlistRequestService: provider.GetRequiredService<MTM_Waitlist.Module_Waitlist.Services.IWaitlistRequestService>(),
            imageLocationService: provider.GetRequiredService<MTM_Waitlist.Module_Settings.Services.IImageLocationService>(),
            dispatcherQueue: DispatcherQueue.GetForCurrentThread(),
            startupState: provider.GetRequiredService<MTM_Waitlist.Module_Core.Models.StartupState>(),
            storeAvailabilityTracker: provider.GetRequiredService<IStoreAvailabilityTracker>(),
            actionPrompt: provider.GetRequiredService<MTM_Waitlist.Module_Waitlist.Services.IWaitlistRequestActionPrompt>(),
            urgencyDeadlineService: provider.GetRequiredService<IUrgencyDeadlineService>(),
            messageSeenStore: provider.GetRequiredService<MTM_Waitlist.Module_Waitlist.Services.IWaitlistMessageSeenStore>(),
            sortPreferenceService: provider.GetRequiredService<IWaitlistSortPreferenceService>(),
            jobAvailabilityProvider: provider.GetRequiredService<MTM_Waitlist.Module_Settings.Services.IRequestJobPartAvailabilityProvider>(),
            permissionService: provider.GetRequiredService<IPermissionService>(),
            partPictureResolver: provider.GetRequiredService<MTM_Waitlist.Module_Core.Contracts.Services.IPartPictureResolver>()));
        services.AddTransient<WaitlistViewPage>();
        services.AddTransient<ShellPage>();
        services.AddTransient<ShellViewModel>();

        // User management and permissions (feature 006). One declaration of every permission, the user-management
        // rules, and the store seam beneath them, all in MTM_Waitlist.Core so the sign-in path, the settings
        // library, the setup library and the separate service host read one implementation rather than a copy.
        services.AddSingleton<IPermissionService, PermissionService>();
        services.AddSingleton<IRoleCatalogService, RoleCatalogService>();
        services.AddSingleton<IUserManagementRepository, UserManagementRepository>();
        services.AddSingleton<IUserManagementService, UserManagementService>();
        services.AddSingleton<IPermissionAdministrationService, PermissionAdministrationService>();
        // Feature 006's pages, one registration file per story phase so three phases never contend on this one.
        // Each declaration below is implemented by its own file; a phase that has not landed yet contributes
        // nothing, and the page is simply not reachable until it does.
        RegisterUserManagementPages(services);
        RegisterUserAccountPages(services);
        RegisterPermissionsPages(services);
        RegisterPartPictureServices(services);
        RegisterPartPictureScreen(services);
        RegisterPartPictureCache(services);

        // Configuration
        services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
        services.Configure<StartupDatabaseOptions>(context.Configuration.GetSection(nameof(StartupDatabaseOptions)));
        services.Configure<ReceivingDatabaseOptions>(context.Configuration.GetSection(nameof(ReceivingDatabaseOptions)));
        services.Configure<StartupDevelopmentOptions>(context.Configuration.GetSection(nameof(StartupDevelopmentOptions)));
        services.Configure<StartupLoggingOptions>(context.Configuration.GetSection(nameof(StartupLoggingOptions)));
        services.Configure<StartupWindowOptions>(context.Configuration.GetSection(nameof(StartupWindowOptions)));

        // Dunnage images (Setup) resolve against the shared Dunnage image root (the same
        // root the MTM Receiving Application writes via "Dunnage.Application.DefaultImageLocation").
        DunnageImagePathResolver.ConfigureRootFolder(context.Configuration["DunnageImageOptions:RootFolder"]);

        // The local picture cache, mirroring both trees the application reads. The waitlist root is a setting, so
        // the factory resolves it when a run starts rather than when the container is built; the Dunnage cache
        // folder is the receiving application's own when this machine already has one, and ours when it does not.
        services.AddSingleton<MTM_Waitlist.Module_Shared.Services.IImageCacheSyncService>(provider =>
            new MTM_Waitlist.Module_Shared.Services.ImageCacheSyncService(async cancellationToken =>
            {
                var storageConfiguration = provider
                    .GetRequiredService<MTM_Waitlist.Module_Settings.Services.IImageStorageConfigurationResolver>();

                // Adopted before anything reads a cached path, so the folder the copy is written into and the
                // folder the screens look in are the same one.
                MTM_Waitlist.Module_Shared.Helpers.ImageCachePaths.SetCacheRoot(
                    await storageConfiguration.GetImageCacheFolderPathAsync().ConfigureAwait(false));

                if (await storageConfiguration.GetImageCacheEnabledAsync().ConfigureAwait(false) is false)
                {
                    // No sources means nothing is copied and nothing is removed: a machine with caching switched
                    // off keeps whatever it already had and reads every picture from the share.
                    return Array.Empty<MTM_Waitlist.Module_Shared.Services.ImageCacheSource>();
                }

                var sources = new List<MTM_Waitlist.Module_Shared.Services.ImageCacheSource>
                {
                    new(
                        "dunnage pictures",
                        DunnageImagePathResolver.RootFolder,
                        MTM_Waitlist.Module_Shared.Helpers.ImageCachePaths.ResolveDunnageCacheFolder()),
                };

                // The image location service is initialized here rather than assumed. This delegate is the first
                // thing to ask it for a path, and it runs inside startup, before any screen has initialized it:
                // asking first threw, the synchroniser swallowed the throw as "nothing to do", and the cache was
                // never built in the one run that is meant to build it — while the Settings screen went on saying
                // that pictures are copied onto this computer when the application starts.
                var imageLocationService = provider
                    .GetRequiredService<MTM_Waitlist.Module_Settings.Services.IImageLocationService>();

                if (await imageLocationService.EnsureInitializedAsync().ConfigureAwait(false))
                {
                    sources.Insert(
                        0,
                        new MTM_Waitlist.Module_Shared.Services.ImageCacheSource(
                            "waitlist pictures",
                            await imageLocationService.GetSharedFolderPathAsync().ConfigureAwait(false),
                            Path.Combine(
                                MTM_Waitlist.Module_Shared.Helpers.ImageCachePaths.LocalCacheRoot,
                                MTM_Waitlist.Module_Shared.Helpers.ImageCachePaths.WaitlistFolderName),

                            // The two part collections are left to the part cache store: a part's picture is
                            // copied when that part is first drawn, not mirrored in one pass at startup (FR-030).
                            MTM_Waitlist.Module_Shared.Helpers.PartPictureLayout.PartCollectionFolders,

                            // The same walk cleans up the pictures a replace has archived, once their retention
                            // period has passed: the period is the store's setting and is read here, per run.
                            await storageConfiguration.GetArchiveKeepDaysAsync().ConfigureAwait(false)));
                }
                else
                {
                    // One source skipped and one kept, rather than both lost: the Dunnage tree is still mirrored
                    // from its own setting, and the waitlist pictures are read from the share for this run. The
                    // service has already logged why it could not initialize.
                    MTM_Waitlist.Module_Core.Helpers.StartupDebugLog.Info(
                        "ImageCache",
                        "The waitlist picture source is skipped for this run because the image locations could not be initialized; those pictures are read from the share.");
                }

                return sources;
            }));

        services.AddModuleServices(context.Configuration);

        return services;
    }

    /// <summary>
    /// Registers the user-list page and its view model, in the story phase that builds them.
    /// </summary>
    static partial void RegisterUserManagementPages(IServiceCollection services);

    /// <summary>
    /// Registers the create page, the person's page and the one-time PIN window, in the story phase that builds
    /// them.
    /// </summary>
    static partial void RegisterUserAccountPages(IServiceCollection services);

    /// <summary>
    /// Registers the permissions page and the who-holds-this view, in the story phase that builds them.
    /// </summary>
    static partial void RegisterPermissionsPages(IServiceCollection services);

    /// <summary>
    /// Feature 008's own services: the part-picture reader, and later the part-picture screen and the local copy
    /// store. Each is implemented in its own file beside this one, so no phase edits the factory the others edit.
    /// </summary>
    static partial void RegisterPartPictureServices(IServiceCollection services);

    /// <summary>
    /// Feature 008's part-picture screen: the write path, the coverage read, the view model and the page route.
    /// </summary>
    static partial void RegisterPartPictureScreen(IServiceCollection services);

    /// <summary>
    /// Feature 008's local copy store: this computer's copy of one part's picture, made the first time that part is
    /// drawn rather than in one pass at startup.
    /// </summary>
    static partial void RegisterPartPictureCache(IServiceCollection services);
}
