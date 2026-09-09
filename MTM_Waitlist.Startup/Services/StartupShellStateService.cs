using Microsoft.Extensions.Options;
using Microsoft.UI.Windowing;

using Windows.Graphics;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Startup.Services;

public sealed class StartupShellStateService : IStartupShellStateService
{
    private readonly StartupWindowOptions _windowOptions;
    private readonly IAppWindowProvider _appWindowProvider;

    public StartupShellStateService(IOptions<StartupWindowOptions> windowOptions, IAppWindowProvider appWindowProvider)
    {
        _windowOptions = windowOptions?.Value ?? new StartupWindowOptions();
        _appWindowProvider = appWindowProvider;
    }

    public event EventHandler? StateChanged;

    public bool IsNavigationVisible { get; private set; }

    public void EnterSplashMode()
    {
        IsNavigationVisible = false;
        TryResizeWindow(_windowOptions.SplashWidth, _windowOptions.SplashHeight, _windowOptions.CenterOnModeSwitch);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task EnterMainModeAsync(CancellationToken cancellationToken = default)
    {
        var delay = Math.Max(0, _windowOptions.MainTransitionDelayMilliseconds);
        if (delay > 0)
        {
            await Task.Delay(delay, cancellationToken);
        }

        IsNavigationVisible = true;
        MaximizeMainWindow();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Maximizes the main window when the app transitions into the main shell so the app
    /// launches full-screen on the shop floor. Falls back to the configured window size
    /// when the window presenter cannot be maximized.
    /// </summary>
    private void MaximizeMainWindow()
    {
        try
        {
            var appWindow = _appWindowProvider.MainWindow.AppWindow;
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
                return;
            }
        }
        catch
        {
            // Fall through to the configured window sizing below.
        }

        TryResizeWindow(_windowOptions.MainWidth, _windowOptions.MainHeight, _windowOptions.CenterOnModeSwitch);
    }

    private void TryResizeWindow(int width, int height, bool centerWindow)
    {
        try
        {
            var appWindow = _appWindowProvider.MainWindow.AppWindow;
            var clampedWidth = Math.Max(400, width);
            var clampedHeight = Math.Max(300, height);

            if (centerWindow)
            {
                var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
                var workArea = displayArea.WorkArea;
                var x = workArea.X + (workArea.Width - clampedWidth) / 2;
                var y = workArea.Y + (workArea.Height - clampedHeight) / 2;
                appWindow.MoveAndResize(new RectInt32(x, y, clampedWidth, clampedHeight));
                return;
            }

            appWindow.Resize(new SizeInt32(clampedWidth, clampedHeight));
        }
        catch
        {
            // Ignore sizing failures during startup edge cases.
        }
    }
}
