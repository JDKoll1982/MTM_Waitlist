using Microsoft.UI.Xaml;

namespace MTM_Waitlist.GenericRequest.Temp;

/// <summary>
/// The preview application. It opens one window and reads nothing.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
