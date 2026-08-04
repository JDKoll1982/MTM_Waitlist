using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace MTM_Waitlist.Module_Startup.Views;

public sealed partial class LoginWindow : WindowEx
{
    private readonly LoginPage _loginPage;

    public LoginWindow()
    {
        InitializeComponent();
        _loginPage = new LoginPage();
        Content = _loginPage;
        Title = "Sign in";
        ApplyLoginWindowSize();
        Activated += OnActivated;
    }

    private void OnActivated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
    {
        ApplyLoginWindowSize();
    }

    private void ApplyLoginWindowSize()
    {
        try
        {
            var width = 820;
            var height = 760;

            var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
            var workArea = displayArea.WorkArea;
            var x = workArea.X + ((workArea.Width - width) / 2);
            var y = workArea.Y + ((workArea.Height - height) / 2);

            AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
        }
        catch
        {
            // Keep startup flowing even if sizing fails on specific environments.
        }
    }
}
