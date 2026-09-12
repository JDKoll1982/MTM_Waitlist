using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Mock.Service.Contracts;
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

        ConfigureTitleBar();
        ConfigureSearchBox();

        // The search box lives in the window, the meaning of a search lives in the page, so the two are
        // re-synchronised after each navigation rather than at navigation time.
        ContentFrame.Navigated += OnContentFrameNavigated;

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

    /// <summary>The active surface's search target, or <see langword="null"/> when it cannot be searched.</summary>
    private IServiceSearchTarget? CurrentSearchTarget =>
        (ContentFrame.Content as IServiceSearchHost)?.SearchTarget;

    /// <summary>
    /// Replaces the system title bar with the window's own, carrying the app icon, the title, and the
    /// search box — the shape the main application uses.
    /// </summary>
    /// <remarks>
    /// <c>ExtendsContentIntoTitleBar</c> has to be set from code (setting it in XAML is an error) and it
    /// has to be set before the title bar element is registered, otherwise the system title bar stays.
    /// </remarks>
    private void ConfigureTitleBar()
    {
        ExtendsContentIntoTitleBar = true;

        ShellTitleBar.Title = "Service_Shell.Title".GetLocalized();

        SetTitleBar(ShellTitleBar);

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            // The title bar holds a search box, which needs more room than the standard caption height.
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        }

        // The icon is set on the window rather than through IconSource, because SetIcon is the documented
        // route for an .ico and the file already ships beside the executable.
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "WindowIcon.ico");

        if (File.Exists(iconPath))
        {
            AppWindow.SetIcon(iconPath);
            return;
        }

        ServiceLog.Info("ServiceShellWindow", $"Window icon was not found at '{iconPath}'.");
    }

    /// <summary>Applies the localized text the title-bar search box needs.</summary>
    private void ConfigureSearchBox()
    {
        TitleBarSearchBox.PlaceholderText = "Service_Shell.SearchPlaceholder".GetLocalized();
        AutomationProperties.SetName(TitleBarSearchBox, "Service_Shell.SearchName".GetLocalized());
    }

    /// <summary>
    /// Points the search box at the surface that has just loaded, restoring that surface's own search text.
    /// </summary>
    private void OnContentFrameNavigated(object sender, NavigationEventArgs e)
    {
        var target = CurrentSearchTarget;

        TitleBarSearchBox.Text = target?.SearchQuery ?? string.Empty;

        if (target is null)
        {
            TitleBarSearchBox.ItemsSource = null;
            return;
        }

        target.UpdateSearchSuggestions(target.SearchQuery);
        TitleBarSearchBox.ItemsSource = target.SearchSuggestions;
    }

    private void OnTitleBarSearchBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        // Only the operator's typing narrows a surface; restoring the box during navigation must not.
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        var target = CurrentSearchTarget;

        if (target is null)
        {
            sender.ItemsSource = null;
            return;
        }

        target.UpdateSearchSuggestions(sender.Text);
        sender.ItemsSource = target.SearchSuggestions;
    }

    private void OnTitleBarSearchBoxSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is string suggestion)
        {
            sender.Text = suggestion;
        }
    }

    private void OnTitleBarSearchBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) =>
        CurrentSearchTarget?.SubmitSearch(args.QueryText, args.ChosenSuggestion as string);

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
