using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// The user list (FR-085 to FR-096).
/// </summary>
/// <remarks>
/// The page holds no rule of its own: it draws the view model's five states, it names the five columns, and every
/// control it shows is one the reader may use, because the view model only enables them from the permission it
/// read when the page was reached. Ordering and paging are the view model's too: a column heading is a command
/// that names its own column, so there is exactly one place where the roster's order is decided.
/// </remarks>
public sealed partial class UserManagementPage : Page
{
    public UserManagementViewModel ViewModel
    {
        get;
    }

    public UserManagementPage()
    {
        StartupDebugLog.Info("UserManagementPage", "Constructor started.");
        ViewModel = App.GetService<UserManagementViewModel>();
        InitializeComponent();
        StartupDebugLog.Info("UserManagementPage", "Constructor completed.");
    }

    /// <summary>Back returns to the Settings screen this page was opened from.</summary>
    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    /// <summary>Reaching a person's row opens that person: the row's whole job (FR-087).</summary>
    private void OnPersonInvoked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is UserSummary person)
        {
            ViewModel.OpenPersonCommand.Execute(person);
        }
    }

    /// <summary>
    /// The same trip, for the reader who tabs to the person's name: the row is reachable with a mouse or a
    /// keyboard, and both go to the same place.
    /// </summary>
    private void OnPersonLinkClick(object sender, RoutedEventArgs e)
    {
        if (sender is HyperlinkButton { DataContext: UserSummary person })
        {
            ViewModel.OpenPersonCommand.Execute(person);
        }
    }
}
