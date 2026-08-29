using Microsoft.Extensions.Options;
using Microsoft.UI.Windowing;

using Windows.Graphics;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Startup.Services;

public sealed class StartupShellStateService : IStartupShellStateService
{
    private readonly StartupWindowOptions _windowOptions;

    public StartupShellStateService(IOptions<StartupWindowOptions> windowOptions)
    {
        _windowOptions = windowOptions?.Value ?? new StartupWindowOptions();
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
        TryResizeWindow(_windowOptions.MainWidth, _windowOptions.MainHeight, _windowOptions.CenterOnModeSwitch);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void TryResizeWindow(int width, int height, bool centerWindow)
    {
        try
        {
            var appWindow = App.MainWindow.AppWindow;
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
