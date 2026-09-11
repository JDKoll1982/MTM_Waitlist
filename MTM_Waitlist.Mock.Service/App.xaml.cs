using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Service.Services;
using MTM_Waitlist.Mock.Service.Views;
using WinUIEx;

namespace MTM_Waitlist.Mock.Service;

/// <summary>
/// Entry point and lifetime for the on-host Module_Mock service.
/// </summary>
/// <remarks>
/// <para>
/// <b>Tray-only, single instance.</b> The service creates no window at startup: it registers a tray icon
/// and starts its background engines. A second launch redirects to the running instance instead of
/// starting a competing copy — two instances would contend over the same stage tables and could report a
/// partially loaded mirror as the current one (FR-007, research.md R3).
/// </para>
/// <para>
/// <b>Unpackaged-safe.</b> Auto-start uses the per-user <c>Run</c> key rather than the MSIX-only
/// <c>StartupTask</c> API, so no packaging guard is needed anywhere (research.md R4).
/// </para>
/// <para>
/// <b>The window is optional and hides.</b> Closing it hides it; the process exits only on an explicit
/// Quit from the tray menu, which disposes the icon that keeps the process alive.
/// </para>
/// </remarks>
public partial class App : Application
{
    /// <summary>Single-instance key, unique to this service.</summary>
    public const string SingleInstanceKey = "MTM_Waitlist.Mock.Service";

    private static App? s_instance;

    private ServiceHostBuilder? _hostBuilder;
    private ServiceProvider? _services;
    private TrayIcon? _trayIcon;
    private ServiceShellWindow? _shellWindow;
    private CancellationTokenSource? _shutdownSource;

    /// <summary>
    /// A surface a launch asked for but that has not been shown yet, because the container did not exist
    /// when the request arrived.
    /// </summary>
    private ServiceActivationParser.RequestedSurface _pendingSurface = ServiceActivationParser.RequestedSurface.None;

    /// <summary>
    /// The UI thread's dispatcher, captured at launch.
    /// </summary>
    /// <remarks>
    /// A show request is served from a background wait, which is not the UI thread, and
    /// <c>Application</c> does not expose a dispatcher queue in this Windows App SDK version, so the queue
    /// is taken here - the one place that is guaranteed to be the UI thread - and used to marshal the
    /// window work.
    /// </remarks>
    private Microsoft.UI.Dispatching.DispatcherQueue? _uiDispatcher;

    /// <summary>The event a second launch signals to ask for the status surface.</summary>
    private readonly EventWaitHandle _showStatusRequest =
        new(false, EventResetMode.AutoReset, ServiceShowChannel.StatusEventName);

    /// <summary>The event a second launch signals to ask for the settings surface.</summary>
    private readonly EventWaitHandle _showSettingsRequest =
        new(false, EventResetMode.AutoReset, ServiceShowChannel.SettingsEventName);

    /// <summary>The background wait that serves those requests.</summary>
    private Task? _showRequestListener;

    /// <summary>Creates the app.</summary>
    public App()
    {
        s_instance = this;
        InitializeComponent();

        UnhandledException += OnUnhandledException;
    }

    /// <summary>
    /// Resolves a service from the running container.
    /// </summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    /// <returns>The resolved service.</returns>
    /// <exception cref="InvalidOperationException">The container has not been built yet.</exception>
    public static T GetService<T>()
        where T : notnull
    {
        var services = s_instance?._services
            ?? throw new InvalidOperationException("The service container has not been built yet.");

        return services.GetRequiredService<T>();
    }

    /// <inheritdoc />
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // A launch that asks for a surface (the desktop shortcut's "Show UI") is served once the engines
        // are up. A bare launch - logon auto-start, or the deployment health check - stays tray-only,
        // which is the documented lifetime and what that health check asserts.
        //
        // An unpackaged launch does not reliably carry the command line in the WinUI arguments, so the
        // process command line is consulted as well: Environment.GetCommandLineArgs is the documented
        // route for raw arguments on a plain Launch activation.
        var requestedSurface = ServiceActivationParser.Parse(args?.Arguments);
        if (requestedSurface == ServiceActivationParser.RequestedSurface.None)
        {
            requestedSurface = ServiceActivationParser.ParseTokens(Environment.GetCommandLineArgs().Skip(1));
        }

        _pendingSurface = requestedSurface;

        _uiDispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        _ = StartAsync();
    }

    /// <summary>
    /// Enforces single-instance, loads configuration, starts the engines, and registers the tray icon.
    /// </summary>
    private async Task StartAsync()
    {
        try
        {
            var appInstance = AppInstance.FindOrRegisterForKey(SingleInstanceKey);

            if (!appInstance.IsCurrent)
            {
                // A second launch asks the running service for the surface its command line named. The
                // activation handed over below does not carry the command line for an unpackaged app, so
                // this process reads its own and signals the service directly.
                ServiceShowChannel.Request(
                    ServiceActivationParser.ParseTokens(Environment.GetCommandLineArgs().Skip(1)));

                // Then hand the activation over and exit, so only one instance is ever running (FR-007).
                await appInstance
                    .RedirectActivationToAsync(AppInstance.GetCurrent().GetActivatedEventArgs())
                    .AsTask()
                    .ConfigureAwait(true);

                Exit();
                return;
            }

            // Before the engines, so a request that arrives during startup is served rather than dropped.
            _shutdownSource = new CancellationTokenSource();

            // Started before the engines, so a request that arrives during startup is served rather than
            // dropped. It has to come after the cancellation source exists: the listener takes that token
            // to decide when to stop, and a listener that starts without it exits immediately.
            StartShowRequestListener(_shutdownSource.Token);

            _hostBuilder = ServiceHostBuilder.Create();
            var configuration = await _hostBuilder.LoadConfigurationAsync().ConfigureAwait(true);

            // Reconcile the auto-start setting against the real per-user Run entry before the container is
            // built, so the status surface can report a mismatch instead of hiding it (FR-007).
            var configurationStore = _hostBuilder.ConfigurationStore!;
            var autoStartState = await configurationStore
                .ReconcileAutoStartAsync(new CurrentUserRunRegistrationStore())
                .ConfigureAwait(true);

            StartupDebugLog.Info("ServiceApp", $"Auto-start reconciliation: {autoStartState.Message}");

            _services = _hostBuilder.Build(configuration, autoStartState: autoStartState);

            // Tray first, engines second: the operator's route to configuring the service must exist even when
            // the engines cannot do any work yet (an unconfigured cache, an unreachable cache, a missing
            // mysqldump). Creating the icon afterwards would mean a startup problem left no UI at all.
            CreateTrayIcon();

            await StartBackgroundEnginesAsync().ConfigureAwait(true);

            // Served last, so the window never opens onto a service that cannot answer it yet.
            ShowRequestedSurface();
        }
        catch (Exception exception)
        {
            StartupDebugLog.Error("ServiceApp", exception, "The service failed to start.");
            Exit();
        }
    }

    /// <summary>
    /// Loads durable state, validates the shape catalog, and starts the refresh loop, backup schedule,
    /// and API host.
    /// </summary>
    /// <remarks>
    /// Every engine is started independently: an API that cannot bind, or a cache that cannot be read,
    /// must not stop the refresh or backup work. A shape that fails validation is excluded and reported
    /// rather than crashing the service (FR-020). A failure here is logged and the tray (already created)
    /// keeps the operator able to configure the service and quit it.
    /// </remarks>
    private async Task StartBackgroundEnginesAsync()
    {
        var services = _services!;
        var cancellationToken = _shutdownSource!.Token;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("ServiceApp");

        try
        {
            await services.GetRequiredService<RefreshRunRecordStore>().LoadAsync(cancellationToken).ConfigureAwait(true);
            await services.GetRequiredService<BackupArtifactStore>().LoadAsync(cancellationToken).ConfigureAwait(true);

            // Resolving the recorder subscribes the durable store to the engine's completed runs, which is
            // what makes per-item last-refresh status real (FR-013). Held for the process lifetime.
            _ = services.GetRequiredService<RefreshRunRecordRecorder>();

            var catalogProvider = services.GetRequiredService<RefreshShapeCatalogProvider>();
            var entries = await catalogProvider.ValidateAsync(cancellationToken).ConfigureAwait(true);

            foreach (var entry in entries.Where(entry => !entry.IsValid))
            {
                StartupDebugLog.Info("ServiceApp", $"Shape '{entry.Shape.Key}' was excluded: {entry.InvalidReason}");
            }

            foreach (var unregisteredKey in catalogProvider.UnregisteredShapeKeys)
            {
                StartupDebugLog.Info(
                    "ServiceApp",
                    $"Shape '{unregisteredKey}' exists in the cache but is not registered in the catalog, so nothing refreshes it.");
            }

            var timeProvider = services.GetRequiredService<TimeProvider>();

            _ = Task.Run(
                () => services.GetRequiredService<RefreshEngine>().RunScheduledAsync(timeProvider, cancellationToken),
                CancellationToken.None);

            _ = Task.Run(
                async () =>
                {
                    try
                    {
                        await services.GetRequiredService<BackupScheduler>()
                            .RunScheduledAsync(timeProvider, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "The scheduled backup loop ended unexpectedly.");
                    }
                },
                CancellationToken.None);

            var apiHost = services.GetRequiredService<ServiceApiHost>();
            var apiStarted = await apiHost.StartAsync(cancellationToken).ConfigureAwait(true);

            logger.LogInformation(
                "Service started. Refresh loop running, backup schedule running, API {ApiState}.",
                apiStarted ? $"listening on {apiHost.BindEndpoint}" : "not listening");
        }
        catch (Exception exception)
        {
            // The tray is already up, so this is reported rather than fatal: an operator can open Settings,
            // fix the configuration, and restart the service from the tray menu.
            logger.LogError(
                exception,
                "The background engines could not be started. The tray and settings surfaces remain available.");
        }
    }

    /// <summary>
    /// Creates the tray icon and its menu: open status, open settings, back up now, quit.
    /// </summary>
    private void CreateTrayIcon()
    {
        _trayIcon = new TrayIcon(
            1,
            Path.Combine(AppContext.BaseDirectory, "Assets", "WindowIcon.ico"),
            "Service_Tray.Tooltip".GetLocalized())
        {
            IsVisible = true
        };

        _trayIcon.Selected += (_, _) => ShowWindow(showSettings: false);

        _trayIcon.ContextMenu += (_, e) =>
        {
            var flyout = new MenuFlyout();

            var statusItem = new MenuFlyoutItem { Text = "Service_Tray.OpenStatus".GetLocalized() };
            statusItem.Click += (_, _) => ShowWindow(showSettings: false);

            var settingsItem = new MenuFlyoutItem { Text = "Service_Tray.OpenSettings".GetLocalized() };
            settingsItem.Click += (_, _) => ShowWindow(showSettings: true);

            var backupItem = new MenuFlyoutItem { Text = "Service_Tray.BackupNow".GetLocalized() };
            backupItem.Click += async (_, _) => await RunManualBackupAsync().ConfigureAwait(true);

            var quitItem = new MenuFlyoutItem { Text = "Service_Tray.Quit".GetLocalized() };
            quitItem.Click += (_, _) => Shutdown();

            flyout.Items.Add(statusItem);
            flyout.Items.Add(settingsItem);
            flyout.Items.Add(new MenuFlyoutSeparator());
            flyout.Items.Add(backupItem);
            flyout.Items.Add(new MenuFlyoutSeparator());
            flyout.Items.Add(quitItem);

            e.Flyout = flyout;
        };
    }

    /// <summary>Shows the window on one surface, creating it on first use.</summary>
    /// <param name="showSettings"><see langword="true"/> for settings, <see langword="false"/> for status.</param>
    private void ShowWindow(bool showSettings)
    {
        _shellWindow ??= new ServiceShellWindow(_services!);
        _shellWindow.NavigateTo(showSettings);
    }
    /// <summary>
    /// Starts waiting for a second launch to ask for a surface.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the service is shutting down.</param>
    /// <remarks>
    /// The wait is a background loop with a short timeout so shutdown is noticed promptly, and the window
    /// work is marshalled to the UI thread. A request that arrives before the container exists is held and
    /// served once it does, so a launch during startup still opens the window that was asked for.
    /// </remarks>
    private void StartShowRequestListener(CancellationToken cancellationToken)
    {
        _showRequestListener = Task.Run(() =>
        {
            WaitHandle[] requests = [_showStatusRequest, _showSettingsRequest];

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var signalled = WaitHandle.WaitAny(requests, TimeSpan.FromSeconds(1));

                    if (signalled == WaitHandle.WaitTimeout)
                    {
                        continue;
                    }

                    var showSettings = signalled == 1;

                    _uiDispatcher?.TryEnqueue(() =>
                    {
                        if (_services is null)
                        {
                            _pendingSurface = showSettings
                                ? ServiceActivationParser.RequestedSurface.Settings
                                : ServiceActivationParser.RequestedSurface.Status;
                            return;
                        }

                        ShowWindow(showSettings);
                    });
                }
            }
            catch (ObjectDisposedException)
            {
                // Shutdown disposed the handles while this was waiting; the process is ending.
            }
        });
    }

    /// <summary>Opens the window a launch asked for, when it asked for one.</summary>
    private void ShowRequestedSurface()
    {
        var surface = _pendingSurface;
        _pendingSurface = ServiceActivationParser.RequestedSurface.None;

        if (surface == ServiceActivationParser.RequestedSurface.None)
        {
            return;
        }

        ShowWindow(surface == ServiceActivationParser.RequestedSurface.Settings);
    }

    /// <summary>
    /// Backs up the application's own store immediately — the tray's "back up now" action.
    /// </summary>
    private async Task RunManualBackupAsync()
    {
        try
        {
            var record = await GetService<BackupEngine>()
                .RunAsync(BackupStore.MtmWaitlist)
                .ConfigureAwait(true);

            StartupDebugLog.Info("ServiceApp", $"Manual backup for mtm_waitlist finished as {record.Outcome}.");
        }
        catch (Exception exception)
        {
            StartupDebugLog.Error("ServiceApp", exception, "A manual backup failed.");
        }
    }

    /// <summary>
    /// Stops the engines, disposes the tray icon, and ends the process.
    /// </summary>
    /// <remarks>
    /// Disposing the tray icon is what lets the process exit: the icon is what keeps a tray-only
    /// application alive once no window is open.
    /// </remarks>
    private void Shutdown()
    {
        try
        {
            _shutdownSource?.Cancel();

            // Let the show-request listener notice the cancellation before its handles are disposed.
            _showRequestListener?.Wait(TimeSpan.FromSeconds(2));

            if (_services?.GetService<ServiceApiHost>() is { IsRunning: true } apiHost)
            {
                apiHost.StopAsync().GetAwaiter().GetResult();
            }
        }
        catch (Exception exception)
        {
            StartupDebugLog.Error("ServiceApp", exception, "The service did not shut down cleanly.");
        }
        finally
        {
            // Allow the window to close for real during shutdown, then release the tray icon.
            if (_shellWindow is not null)
            {
                _shellWindow.AllowClose();
                _shellWindow.Close();
            }

            _showStatusRequest.Dispose();
            _showSettingsRequest.Dispose();
            _trayIcon?.Dispose();
            _services?.Dispose();
            Exit();
        }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        StartupDebugLog.Error("ServiceApp", e.Exception, "An unhandled exception reached the service application.");
    }
}
