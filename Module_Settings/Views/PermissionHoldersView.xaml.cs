using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// The who-holds-this view, hosted on the permissions page (FR-075 to FR-079).
/// </summary>
/// <remarks>
/// The control holds no rule of its own. It loads the feature list from the declaration, reads the answer for the
/// chosen feature, and opens a differing person's own page, because that is the only place a person is changed.
/// </remarks>
public sealed partial class PermissionHoldersView : UserControl
{
    public PermissionHoldersViewModel ViewModel
    {
        get;
    }

    public PermissionHoldersView()
    {
        ViewModel = App.GetService<PermissionHoldersViewModel>();
        InitializeComponent();

        Loaded += OnLoaded;
    }

    /// <summary>Fills the picker and reads the first answer when the control reaches the tree.</summary>
    private async void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        StartupDebugLog.Info("PermissionHoldersView", "Reached the tree; reading who holds the first feature.");

        ViewModel.LoadFeatures();
        await ViewModel.LoadAsync();
    }

    /// <summary>Choosing a differing person opens their own page (FR-077).</summary>
    private void OnPersonInvoked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is PermissionHolderRow person)
        {
            ViewModel.OpenPersonCommand.Execute(person);
        }
    }
}
