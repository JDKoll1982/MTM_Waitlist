using MTM_Waitlist.ViewModels;
using MTM_Waitlist.Helpers;
using Microsoft.UI.Xaml;

namespace MTM_Waitlist.Views;

public sealed partial class SplashWindow : WindowEx
{
    private readonly SplashView _splashView;
    private bool _startupTriggered;

    public SplashWindow()
    {
        InitializeComponent();
        _splashView = new SplashView();
        Content = _splashView;
        Activated += SplashWindow_Activated;
        StartupDebugLog.Info("SplashWindow", "SplashWindow initialized and activation handler wired.");
    }

    private async void SplashWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        StartupDebugLog.Info("SplashWindow", "SplashWindow activated event fired.");

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
            StartupDebugLog.Error("SplashWindow", ex, "Splash startup task failed.");
            throw;
        }
    }
}
