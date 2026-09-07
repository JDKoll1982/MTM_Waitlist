using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Services.MockMode;

/// <summary>
/// App-side coordinator for the mock-mode polling host (Workflow 13 subphases 1.3 &amp; 2.2). Once the shell is up
/// it starts the singleton <see cref="IMockModePollingHost"/> (which drives the central mock-config refresh on a
/// timer) and subscribes to its <see cref="IMockModePollingHost.ModeChanged"/> event so each notify-worthy
/// <see cref="MockModeChange"/> renders a taskbar toast naming the affected source.
/// </summary>
/// <remarks>
/// Toasts require an MSIX identity, so an unpackaged app is a no-op (mirroring the existing app-notification and
/// alert gates). Starting is idempotent — a transient/recreated shell can safely call <see cref="Start"/> again.
/// </remarks>
public sealed class MockModeToastCoordinator
{
    private readonly IMockModePollingHost _host;
    private readonly IAppNotificationService _notifications;
    private bool _started;

    public MockModeToastCoordinator(
        IMockModePollingHost host,
        IAppNotificationService notifications)
    {
        _host = host;
        _notifications = notifications;
    }

    /// <summary>True while the underlying polling host is running.</summary>
    public bool IsRunning => _host.IsRunning;

    /// <summary>
    /// Starts mock-mode polling and toast subscription exactly once for packaged apps. Unpackaged apps are skipped
    /// (no-op) because taskbar toasts require MSIX identity.
    /// </summary>
    public void Start()
    {
        if (_started)
        {
            return;
        }

        if (!RuntimeHelper.IsMSIX)
        {
            StartupDebugLog.Info("MockModeToast", "Skipping mock-mode polling host: app is not packaged.");
            return;
        }

        _started = true;
        _host.ModeChanged += OnModeChanged;
        _host.Start();
        StartupDebugLog.Info("MockModeToast", "Mock-mode polling host started.");
    }

    private void OnModeChanged(object? sender, MockModeChange change)
    {
        if (change is null || !change.ShouldNotify)
        {
            return;
        }

        var message = MockModeSummaryProvider.ToastMessage(change);
        StartupDebugLog.Info("MockModeToast", $"Mock-mode change: {message}");
        _notifications.Show(message);
    }
}
