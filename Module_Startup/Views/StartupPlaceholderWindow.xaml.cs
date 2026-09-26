using Microsoft.UI.Xaml;

using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Startup.Views;

/// <summary>
/// The one window a launch produces while the startup surface is being rebuilt: it states that startup was
/// removed and offers the single way out of the process.
/// </summary>
/// <remarks>
/// It exists so the application launches to something honest instead of a half-removed shell. It navigates
/// nowhere and reads no store, and every way of closing it ends the process so nothing is left holding a store
/// connection. The rebuilt launch pipeline replaces it.
/// </remarks>
public sealed partial class StartupPlaceholderWindow : WindowEx
{
    /// <summary>The stable automation id the launch checks address the close button by.</summary>
    public const string CloseButtonAutomationId = "StartupPlaceholder_CloseButton";

    /// <summary>Guards the shutdown path so the close button and the window's own close do not both run it.</summary>
    private static bool s_isClosing;

    public StartupPlaceholderWindow()
    {
        InitializeComponent();

        MessageText.Text = "Startup_Placeholder.Message".GetLocalized();
        CloseButton.Content = "Startup_Placeholder.Close".GetLocalized();

        Closed += StartupPlaceholderWindow_Closed;
    }

    private async void CloseButton_Click(object sender, RoutedEventArgs e) => await CloseApplicationAsync();

    private async void StartupPlaceholderWindow_Closed(object sender, WindowEventArgs args) => await CloseApplicationAsync();

    /// <summary>
    /// Stops the host and ends the process. Bounded and idempotent: the host stop is awaited so no background
    /// service is still writing when the process goes away, and the second caller is a no-op.
    /// </summary>
    private static async Task CloseApplicationAsync()
    {
        if (s_isClosing)
        {
            return;
        }

        s_isClosing = true;

        await App.ShutdownHostAsync().ConfigureAwait(true);
        App.ExitApplication();
    }
}
