using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Services;
namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// The service's composition root: builds the container every engine in the tray app resolves from.
/// </summary>
/// <remarks>
/// <para>
/// Two rules shape this wiring. First, <b>the cache connection is resolved, never configured</b>:
/// <see cref="MySqlConnectionStringResolver"/> reads the same environment variables the application uses,
/// so no credential is duplicated into the service's settings file (FR-026). Second,
/// <b><c>RestoreService</c> is registered independently of the API host</b>, so the network surface can
/// start with restore absent and there is no code path from an HTTP request to a database restore
/// (FR-023, <c>contracts/mock-service-http-api.md</c> §5).
/// </para>
/// <para>
/// Startup validation is deliberately not fatal: a shape whose artifacts are missing is excluded and
/// reported while the remaining shapes still refresh (FR-020).
/// </para>
/// </remarks>
public sealed class ServiceHostBuilder
{
    /// <summary>Directory name under the service's app-data root that holds backups and settings.</summary>
    public const string AppDataDirectoryName = "MTM_Waitlist.Mock.Service";

    private readonly IServiceCollection _services = new ServiceCollection();

    private ServiceConfigurationStore? _configurationStoreInstance;

    private Models.ServiceConfiguration _loadedConfiguration =
        Models.ServiceConfiguration.CreateDefault(AppDataDirectoryName);

    private ServiceHostBuilder(string appDataRoot, string contentRoot)
    {
        AppDataRoot = appDataRoot;
        ContentRoot = contentRoot;
    }

    /// <summary>The service's own app-data folder (settings, run records, backups).</summary>
    public string AppDataRoot { get; }

    /// <summary>Root the Visual queue scripts resolve against.</summary>
    public string ContentRoot { get; }

    /// <summary>
    /// The configuration store created by <see cref="LoadConfigurationAsync"/>, available so the host can
    /// reconcile auto-start against it before the container is built (FR-007).
    /// </summary>
    public ServiceConfigurationStore? ConfigurationStore => _configurationStoreInstance;

    /// <summary>
    /// Creates the default app-data root for the current user.
    /// </summary>
    /// <remarks>
    /// Service-local by design: the configuration and run records must survive a database restore, so
    /// they never live in a store that restore replaces (<c>contracts/mock-service-configuration.md</c> §1).
    /// </remarks>
    public static string GetDefaultAppDataRoot() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppDataDirectoryName);

    /// <summary>
    /// Creates a builder over the given roots.
    /// </summary>
    /// <param name="appDataRoot">Service app-data root; defaults to <see cref="GetDefaultAppDataRoot"/>.</param>
    /// <param name="contentRoot">Script content root; defaults to the running executable's folder.</param>
    public static ServiceHostBuilder Create(string? appDataRoot = null, string? contentRoot = null) =>
        new(
            string.IsNullOrWhiteSpace(appDataRoot) ? GetDefaultAppDataRoot() : appDataRoot,
            string.IsNullOrWhiteSpace(contentRoot) ? AppContext.BaseDirectory : contentRoot);

    /// <summary>
    /// Registers the configuration store and loads it, so later registrations can read the settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded configuration.</returns>
    public async Task<Models.ServiceConfiguration> LoadConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var store = new ServiceConfigurationStore(AppDataRoot);
        var configuration = await store.LoadAsync(cancellationToken).ConfigureAwait(false);

        _configurationStoreInstance = store;
        _services.AddSingleton(store);
        _services.AddSingleton(configuration);

        return configuration;
    }

    /// <summary>
    /// Builds the container.
    /// </summary>
    /// <param name="configuration">The loaded configuration.</param>
    /// <param name="loggerFactory">
    /// Optional logger factory. Supplying one keeps the service's log destination under the host's
    /// control; when omitted a minimal console/debug factory is used.
    /// </param>
    /// <param name="autoStartState">
    /// The startup auto-start reconciliation result, when the host has already reconciled it. Registered
    /// only when present so the status payload can report a mismatch (FR-007).
    /// </param>
    public ServiceProvider Build(
        Models.ServiceConfiguration configuration,
        ILoggerFactory? loggerFactory = null,
        AutoStartReconciliation? autoStartState = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _loadedConfiguration = configuration;

        _services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Information));
        _services.AddSingleton<TimeProvider>(TimeProvider.System);

        if (loggerFactory is not null)
        {
            _services.AddSingleton(loggerFactory);
        }

        if (autoStartState is not null)
        {
            _services.AddSingleton(autoStartState);
        }

        RegisterVisualReadPlumbing(configuration);
        RegisterRefreshPipeline(configuration);
        RegisterBackupPipeline(configuration);
        RegisterServiceApi();

        return _services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });
    }

    /// <summary>
    /// Registers the shared live-read plumbing owned by <c>MTM_Waitlist.Mock</c>, so the service and the
    /// in-app fallback read Infor Visual through exactly one implementation.
    /// </summary>
    /// <param name="configuration">The service configuration.</param>
    private void RegisterVisualReadPlumbing(Models.ServiceConfiguration configuration)
    {
        var visualConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InforVisualDatabaseOptions:Server"] = configuration.VisualSource.Server,
                ["InforVisualDatabaseOptions:Database"] = configuration.VisualSource.Database,
                ["InforVisualDatabaseOptions:User"] = configuration.VisualSource.UserId,
                ["InforVisualDatabaseOptions:ConnectionTimeoutSeconds"] =
                    configuration.VisualSource.ConnectionTimeoutSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)
            })
            .Build();

        _services.AddSingleton<IConfiguration>(visualConfiguration);
        _services.AddSingleton<IInforVisualScriptStore>(_ => new InforVisualScriptStore(ContentRoot));
        _services.AddSingleton<IVisualConnectionStringProvider>(_ => new VisualConnectionStringProvider(visualConfiguration));
        _services.AddSingleton<IVisualConnectivityProbe, VisualConnectivityProbe>();
        _services.AddSingleton<IVisualQueryExecutor, VisualQueryExecutor>();
    }

    /// <summary>
    /// Registers the catalog, payload source, mirror writer, refresh engine, and run-record store.
    /// </summary>
    /// <param name="configuration">The service configuration.</param>
    private void RegisterRefreshPipeline(Models.ServiceConfiguration configuration)
    {
        var resolver = new MySqlConnectionStringResolver(configuration.MySqlConnection);
        _services.AddSingleton(resolver);

        _services.AddSingleton<IVisualShapeMetadataReader>(_ =>
            new VisualShapeMetadataReader(ResolveCacheConnection(resolver)));

        _services.AddSingleton<RefreshShapeCatalogProvider>(_ =>
            new RefreshShapeCatalogProvider(
                new VisualShapeMetadataReader(ResolveCacheConnection(resolver)),
                catalog: null,
                contentRoot: ContentRoot));

        _services.AddSingleton<IVisualShapePayloadSource, VisualShapePayloadSource>();

        _services.AddSingleton<IMockMirrorRefreshWriter>(_ =>
            new MockMirrorRefreshWriter(ResolveCacheConnection(resolver)));

        _services.AddSingleton(_ => new RefreshRunRecordStore(AppDataRoot));

        _services.AddSingleton(provider => new RefreshEngine(
            provider.GetRequiredService<RefreshShapeCatalogProvider>(),
            provider.GetRequiredService<IVisualShapePayloadSource>(),
            provider.GetRequiredService<IMockMirrorRefreshWriter>(),
            provider.GetRequiredService<ILogger<RefreshEngine>>(),
            refreshInterval: configuration.RefreshInterval));

        // Resolving this is what subscribes the store to the engine's completed runs, so per-item last-run
        // status is actually recorded (FR-013). It is a singleton for the service's lifetime.
        _services.AddSingleton(provider => new RefreshRunRecordRecorder(
            provider.GetRequiredService<RefreshEngine>(),
            provider.GetRequiredService<RefreshRunRecordStore>(),
            provider.GetRequiredService<ILogger<RefreshRunRecordRecorder>>()));
    }

    /// <summary>
    /// Reads the configuration currently in force, so a settings save applies without a restart.
    /// </summary>
    /// <returns>The live configuration, or the loaded one when no store instance is attached yet.</returns>
    private Models.ServiceConfiguration GetLiveConfiguration() =>
        _configurationStoreInstance?.Current ?? _loadedConfiguration;

    /// <summary>
    /// Resolves the cache connection, or an empty string when none is configured.
    /// </summary>
    /// <remarks>
    /// An unconfigured cache is <b>not</b> a startup failure. The service must come up with its tray and
    /// settings surface so an operator can point it at MySQL; the condition is then reported by whichever
    /// operation needs the cache (startup validation marks every shape invalid with the reason, a refresh
    /// records a failure, and the status payload reports freshness as unknown). Throwing here instead would
    /// take the process down before any UI existed and leave no in-product way to configure it (FR-012).
    /// </remarks>
    private static string ResolveCacheConnection(MySqlConnectionStringResolver resolver) =>
        resolver.ResolveMockCache() ?? string.Empty;

    /// <summary>
    /// Registers the artifact store, backup engine, scheduler, and the host-only restore service.
    /// </summary>
    /// <param name="configuration">The service configuration.</param>
    /// <remarks>
    /// <see cref="RestoreService"/> is registered here but never resolved by the API host: restore is a
    /// local UI action on the database host, and the two registrations are independent by design so the
    /// network surface can run without it (FR-023).
    /// </remarks>
    private void RegisterBackupPipeline(Models.ServiceConfiguration configuration)
    {
        Func<Models.ServiceConfiguration> configurationAccessor = GetLiveConfiguration;

        _services.AddSingleton<BackupArtifactStore>(_ => new BackupArtifactStore(AppDataRoot));

        _services.AddSingleton(provider => new BackupEngine(
            provider.GetRequiredService<BackupArtifactStore>(),
            provider.GetRequiredService<MySqlConnectionStringResolver>(),
            configurationAccessor,
            provider.GetRequiredService<ILogger<BackupEngine>>()));

        _services.AddSingleton(provider => new BackupScheduler(
            provider.GetRequiredService<BackupEngine>(),
            configurationAccessor,
            provider.GetRequiredService<ILogger<BackupScheduler>>()));

        _services.AddSingleton(provider => new RestoreService(
            provider.GetRequiredService<BackupEngine>(),
            provider.GetRequiredService<BackupArtifactStore>(),
            configurationAccessor,
            provider.GetRequiredService<ILogger<RestoreService>>()));
    }

    /// <summary>
    /// Registers the freshness reader, the operations facade, and the API host.
    /// </summary>
    /// <remarks>
    /// The API's authentication handler resolves <see cref="ServiceConfigurationStore"/> from the API's own
    /// container, so the host passes that same instance in rather than a copy: a rotated credential must be
    /// accepted immediately, without restarting the listener.
    /// </remarks>
    private void RegisterServiceApi()
    {
        _services.AddSingleton<IVisualShapeFreshnessReader>(provider =>
            new VisualShapeFreshnessReader(ResolveCacheConnection(provider.GetRequiredService<MySqlConnectionStringResolver>())));
        _services.AddSingleton(provider => new ServiceApiOperations(
            provider.GetRequiredService<RefreshEngine>(),
            provider.GetRequiredService<RefreshShapeCatalogProvider>(),
            provider.GetRequiredService<RefreshRunRecordStore>(),
            provider.GetRequiredService<BackupEngine>(),
            provider.GetRequiredService<BackupArtifactStore>(),
            provider.GetRequiredService<IVisualShapeFreshnessReader>(),
            provider.GetRequiredService<IVisualConnectivityProbe>(),
            provider.GetRequiredService<ServiceConfigurationStore>(),
            provider.GetService<AutoStartReconciliation>()));

        _services.AddSingleton(provider => new ServiceApiHost(
            provider.GetRequiredService<ServiceApiOperations>(),
            provider.GetRequiredService<ServiceConfigurationStore>(),
            provider.GetRequiredService<ILogger<ServiceApiHost>>()));

        // The settings and status surfaces resolve their view models from this container, so the pages
        // and the API report the same state.
        _services.AddSingleton(provider => new ViewModels.ServiceSettingsViewModel(
            provider.GetRequiredService<ServiceConfigurationStore>(),
            provider.GetRequiredService<BackupEngine>(),
            provider.GetRequiredService<BackupArtifactStore>(),
            provider.GetRequiredService<RestoreService>(),
            provider.GetRequiredService<TimeProvider>()));

        _services.AddSingleton(provider => new ViewModels.ServiceStatusViewModel(
            provider.GetRequiredService<ServiceApiOperations>(),
            provider.GetRequiredService<ServiceConfigurationStore>(),
            provider.GetRequiredService<BackupScheduler>(),
            provider.GetRequiredService<TimeProvider>()));
    }
}
