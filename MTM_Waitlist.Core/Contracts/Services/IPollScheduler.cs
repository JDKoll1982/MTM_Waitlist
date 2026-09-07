namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Timer abstraction that raises periodic ticks for mock-mode polling. In the WinUI app a
/// <see cref="Windows.UI.Xaml.DispatcherTimer"/> (or a manual timer) implements this; tests use a fake that
/// raises ticks on demand. Keeps the polling host unit-testable and decoupled from the UI thread.
/// </summary>
public interface IPollScheduler
{
    /// <summary>Raised on each scheduled interval tick.</summary>
    event EventHandler? Tick;

    /// <summary>Begins ticking. Safe to call once.</summary>
    void Start();

    /// <summary>Stops ticking. Safe to call more than once.</summary>
    void Stop();

    /// <summary>True while the scheduler is ticking.</summary>
    bool IsRunning { get; }
}
