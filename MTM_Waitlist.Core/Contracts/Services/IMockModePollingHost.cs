using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Host that polls the central mock config on a scheduler and raises <see cref="ModeChanged"/> so the app can
/// fire a toast. The WinUI app supplies an <see cref="IPollScheduler"/> backed by a dispatcher/interval timer;
/// tests use a fake scheduler.
/// </summary>
public interface IMockModePollingHost
{
    /// <summary>Raised for each notify-worthy mock-mode change detected on a poll tick.</summary>
    event EventHandler<MockModeChange>? ModeChanged;

    /// <summary>True while the host is polling.</summary>
    bool IsRunning { get; }

    /// <summary>The changes from the most recent poll pass.</summary>
    IReadOnlyList<MockModeChange> LastChanges { get; }

    void Start();

    void Stop();
}
