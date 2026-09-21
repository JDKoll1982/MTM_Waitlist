using CommunityToolkit.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// The user list (FR-085 to FR-096).
/// </summary>
/// <remarks>
/// The page holds no rule of its own: it draws the view model's five states, and every control it shows is one the
/// reader may use, because the view model only enables them from the permission it read when the page was reached.
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

    /// <summary>Reaching a person's row opens that person: the row's whole job.</summary>
    private void OnPersonInvoked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Module_Settings.Models.UserSummary person)
        {
            ViewModel.OpenPersonCommand.Execute(person);
        }
    }
}
