using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist.Mock.Service.ViewModels;

namespace MTM_Waitlist.Mock.Service.Views;

/// <summary>
/// The service settings surface.
/// </summary>
/// <remarks>
/// <para>
/// Every value is validated before it is persisted, so an invalid port or destination is refused with a
/// message rather than accepted and failing later (FR-012). The view model is resolved from the
/// container before the XAML is loaded, which is the repository's page pattern.
/// </para>
/// <para>
/// The restore action is the one interactive flow here: it shows an explicit confirmation dialog and
/// passes the operator's answer to the view model, which does nothing at all when the answer is "no"
/// (FR-010).
/// </para>
/// </remarks>
public sealed partial class ServiceSettingsPage : Page
{
    /// <summary>Creates the page and resolves its view model.</summary>
    public ServiceSettingsPage()
    {
        ViewModel = App.GetService<ServiceSettingsViewModel>();

        InitializeComponent();

        ViewModel.Load();
        ViewModel.LoadRestoreArtifacts();

        _ = ViewModel.RefreshToolStateAsync();
    }

    /// <summary>The page's view model.</summary>
    public ServiceSettingsViewModel ViewModel { get; }

    private void OnBackupStoreSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // The restore picker lists the selected store's artifacts, so it follows the store selection.
        ViewModel.LoadRestoreArtifacts();
    }

    private async void OnRestoreClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedRestoreArtifact is null)
        {
            ViewModel.RestoreSelectedCommand.Execute(false);
            return;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = ViewModel.RestoreConfirmTitleText,
            Content = ViewModel.RestoreConfirmBodyText,
            PrimaryButtonText = ViewModel.RestoreConfirmPrimaryText,
            CloseButtonText = ViewModel.RestoreConfirmCloseText,
            DefaultButton = ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();

        ViewModel.RestoreSelectedCommand.Execute(result == ContentDialogResult.Primary);
    }
}
