using Microsoft.UI.Xaml;
using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Services.MockMode;

/// <summary>
/// WinUI <see cref="DispatcherTimer"/>-backed implementation of <see cref="IPollScheduler"/> used to drive the
/// <c>MockModePollingHost</c> (mock-mode central-config polling). Because it owns a <see cref="DispatcherTimer"/>,
/// it must be created and started on a thread that has a <see cref="DispatcherQueue"/> (the UI thread). Starting is
/// idempotent so a recreated shell can safely call <see cref="Start"/> again.
/// </summary>
public sealed class DispatcherPollScheduler : IPollScheduler
{
    private readonly DispatcherTimer _timer;

    public DispatcherPollScheduler(TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), "Poll interval must be positive.");
        }

        _timer = new DispatcherTimer { Interval = interval };
        _timer.Tick += (_, _) => Tick?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Defaults to a 30-second poll interval.</summary>
    public DispatcherPollScheduler()
        : this(TimeSpan.FromSeconds(30))
    {
    }

    public event EventHandler? Tick;

    public bool IsRunning => _timer.IsEnabled;

    public void Start()
    {
        if (!_timer.IsEnabled)
        {
            _timer.Start();
        }
    }

    public void Stop()
    {
        if (_timer.IsEnabled)
        {
            _timer.Stop();
        }
    }
}
