using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// Drives <see cref="IMockRoutingMonitorService"/> on a scheduler's ticks. Starting the host starts the
/// scheduler; each tick runs one refresh pass and raises <see cref="ModeChanged"/> for every change the monitor
/// emits (so a toast host subscribes here). This is the Phase 2.2 "clients poll on a timer and apply changes +
/// fire a toast" host; the scheduler (e.g. a WinUI <see cref="Windows.UI.Xaml.DispatcherTimer"/>) is injected.
/// </summary>
public sealed class MockModePollingHost : IMockModePollingHost
{
    private readonly IMockRoutingMonitorService _monitor;
    private readonly IPollScheduler _scheduler;

    public MockModePollingHost(IMockRoutingMonitorService monitor, IPollScheduler scheduler)
    {
        _monitor = monitor;
        _scheduler = scheduler;
        _scheduler.Tick += OnTick;
    }

    public event EventHandler<MockModeChange>? ModeChanged
    {
        add => _monitor.MockModeChanged += value;
        remove => _monitor.MockModeChanged -= value;
    }

    public bool IsRunning => _scheduler.IsRunning;

    public IReadOnlyList<MockModeChange> LastChanges => _monitor.LastChanges;

    public void Start()
    {
        StartupDebugLog.Info("MockModePollingHost", "Starting mock-mode polling.");
        _scheduler.Start();
    }

    public void Stop()
    {
        StartupDebugLog.Info("MockModePollingHost", "Stopping mock-mode polling.");
        _scheduler.Stop();
    }

    private async void OnTick(object? sender, EventArgs e)
    {
        try
        {
            await _monitor.RunOnceAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("MockModePollingHost", ex, "Mock-mode refresh pass failed.");
        }
    }
}
