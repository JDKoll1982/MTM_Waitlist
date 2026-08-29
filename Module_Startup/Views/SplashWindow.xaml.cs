using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Startup.ViewModels;
using MTM_Waitlist.Module_Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace MTM_Waitlist.Module_Startup.Views;

public sealed partial class SplashWindow : WindowEx
{
    private readonly SplashView _splashView;
    private bool _startupTriggered;

    public SplashWindow()
    {
        InitializeComponent();
        _splashView = new SplashView();
        Content = _splashView;
        ConfigureSplashChrome();
        ApplySplashWindowSize();
        Activated += SplashWindow_Activated;
        StartupDebugLog.Info("SplashWindow", "SplashWindow initialized and activation handler wired.");
    }

    private void ConfigureSplashChrome()
    {
        try
        {
            if (AppWindow.Presenter is not OverlappedPresenter presenter)
            {
                return;
            }

            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }
        catch
        {
            // Ignore chrome configuration failures and keep startup moving.
        }
    }

    private void ApplySplashWindowSize()
    {
        try
        {
            var options = App.GetService<Microsoft.Extensions.Options.IOptions<StartupWindowOptions>>().Value;
            var width = Math.Max(500, options.SplashWidth);
            var height = Math.Max(360, options.SplashHeight);

            var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
            var workArea = displayArea.WorkArea;
            var x = workArea.X + ((workArea.Width - width) / 2);
            var y = workArea.Y + ((workArea.Height - height) / 2);

            AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
        }
        catch
        {
            // Ignore splash size failures and keep startup moving.
        }
    }

    private async void SplashWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        StartupDebugLog.Info("SplashWindow", "SplashWindow activated event fired.");

        if (!_startupTriggered)
        {
            ApplySplashWindowSize();
        }

        if (_startupTriggered)
        {
            StartupDebugLog.Info("SplashWindow", "Startup already triggered; ignoring activation.");
            return;
        }

        _startupTriggered = true;

        try
        {
            await _splashView.ViewModel.StartAsync();
            StartupDebugLog.Info("SplashWindow", "Splash startup task completed.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SplashWindow", ex, "Splash startup task failed; exiting app.");
            // Do NOT rethrow in async void - exit gracefully instead
            App.Current.Exit();
        }
    }
}
