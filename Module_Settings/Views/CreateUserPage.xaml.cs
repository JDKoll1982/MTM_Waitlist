using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// The create form (FR-099, FR-101, FR-104). A form only: the actions area the person's page carries has no
/// counterpart here, because the only thing this screen does is create.
/// </summary>
public sealed partial class CreateUserPage : Page
{
    public CreateUserViewModel ViewModel
    {
        get;
    }

    public CreateUserPage()
    {
        StartupDebugLog.Info("CreateUserPage", "Constructor started.");
        ViewModel = App.GetService<CreateUserViewModel>();
        InitializeComponent();
        StartupDebugLog.Info("CreateUserPage", "Constructor completed.");
    }

    /// <summary>
    /// Creates the person, then hands the issued credential to the window that reveals it. The reveal window is
    /// the only place the credential exists in readable form, and it is dropped when that window closes.
    /// </summary>
    private async void OnCreateClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.CreateAsync();

        if (ViewModel.IssuedCredential is not { } credential)
        {
            return;
        }

        var dialog = new PinRevealDialog(App.GetService<PinRevealDialogViewModel>());
        dialog.ShowFor(credential, XamlRoot);

        await dialog.ShowAsync();

        ViewModel.DismissIssuedCredential();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => ViewModel.CancelCommand.Execute(null);
}
