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
                // A second launch hands its activation to the running instance and exits.
                await appInstance
                    .RedirectActivationToAsync(AppInstance.GetCurrent().GetActivatedEventArgs())
                    .AsTask()
                    .ConfigureAwait(true);

                Exit();
                return;
            }

            _shutdownSource = new CancellationTokenSource();

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
