using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Mock.Service.Services;

namespace MTM_Waitlist.Mock.Service.Views;

/// <summary>
/// The service's single window: a settings surface and a status surface.
/// </summary>
/// <remarks>
/// <para>
/// Closing this window <b>hides</b> it rather than ending the service: the process exits only through an
/// explicit Quit on the tray menu, which is what makes the tray lifetime meaningful (FR-007, research.md R3).
/// </para>
/// <para>
/// The window is created lazily on first use — the service starts tray-only and never needs a window to
/// refresh the cache or take backups.
/// </para>
/// </remarks>
public sealed partial class ServiceShellWindow : Window
{
    private readonly IServiceProvider _services;

    private bool _allowClose;

    /// <summary>Creates the window.</summary>
    /// <param name="services">The service's container, used to build each page's view model.</param>
    public ServiceShellWindow(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _services = services;

        InitializeComponent();

        Title = "Service_Shell.Title".GetLocalized();

        SettingsNavigationItem.Content = "Service_Shell.NavigationSettings".GetLocalized();
        StatusNavigationItem.Content = "Service_Shell.NavigationStatus".GetLocalized();

        AppWindow.Closing += OnAppWindowClosing;

        ShellNavigationView.SelectedItem = StatusNavigationItem;
    }

    /// <summary>
    /// Navigates the shell to one surface and brings the window to the foreground.
    /// </summary>
    /// <param name="showSettings"><see langword="true"/> for settings, <see langword="false"/> for status.</param>
    public void NavigateTo(bool showSettings)
    {
        ShellNavigationView.SelectedItem = showSettings ? SettingsNavigationItem : StatusNavigationItem;

        // Activate both shows a hidden window and raises an already-visible one.
        Activate();
    }

    /// <summary>
    /// Permits the next close request to actually close the window, for service shutdown.
    /// </summary>
    public void AllowClose() => _allowClose = true;

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item)
        {
            return;
        }

        var isSettings = ReferenceEquals(item, SettingsNavigationItem);

        // A Frame needs a parameterless page constructor; the container travels as the navigation
        // parameter and each page reads it in OnNavigatedTo.
        ContentFrame.Navigate(
            isSettings ? typeof(ServiceSettingsPage) : typeof(ServiceStatusPage),
            _services);
    }

    private void OnAppWindowClosing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        if (_allowClose)
        {
            return;
        }

        // Hide instead of closing: the service must outlive the window (FR-007).
        args.Cancel = true;
        AppWindow.Hide();
    }
}
