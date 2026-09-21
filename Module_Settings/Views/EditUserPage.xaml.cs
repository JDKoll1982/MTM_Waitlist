using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// One person's page (FR-097 to FR-104). The page holds no rule of its own: it draws the view model's state, and
/// every action it offers is one the reader may use, because the view model only enables them from the answers it
/// read when the page was reached.
/// </summary>
public sealed partial class EditUserPage : Page
{
    public EditUserViewModel ViewModel
    {
        get;
    }

    public EditUserPage()
    {
        StartupDebugLog.Info("EditUserPage", "Constructor started.");
        ViewModel = App.GetService<EditUserViewModel>();
        InitializeComponent();
        StartupDebugLog.Info("EditUserPage", "Constructor completed.");
    }

    private void OnBackClick(object sender, RoutedEventArgs e) => ViewModel.GoBackCommand.Execute(null);

    private async void OnSaveClick(object sender, RoutedEventArgs e) => await ViewModel.SaveAsync();

    private async void OnReactivateClick(object sender, RoutedEventArgs e) => await ViewModel.ReactivateAsync();

    /// <summary>
    /// Switches the person off, after a confirmation that names both halves of what that means. The same sentence
    /// stays on the page below the actions, so it is findable after the confirmation is gone.
    /// </summary>
    private async void OnDeactivateClick(object sender, RoutedEventArgs e)
    {
        var confirm = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = ViewModel.DeactivateLabelText,
            Content = ViewModel.DeactivateExplanationText,
            PrimaryButtonText = ViewModel.DeactivateLabelText,
            CloseButtonText = ViewModel.BackLabelText,
            DefaultButton = ContentDialogButton.Close,
        };

        if (await confirm.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        await ViewModel.DeactivateAsync();
    }

    /// <summary>
    /// Issues a fresh credential behind a confirmation, then hands it to the window that reveals it. The reveal
    /// window is the only place the credential exists in readable form, and it is dropped when that window closes.
    /// </summary>
    private async void OnResetClick(object sender, RoutedEventArgs e)
    {
        var confirm = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = ViewModel.ResetPasswordLabelText,
            Content = ViewModel.ResetConfirmationText,
            PrimaryButtonText = ViewModel.ResetPasswordLabelText,
            CloseButtonText = ViewModel.BackLabelText,
            DefaultButton = ContentDialogButton.Close,
        };

        if (await confirm.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        await ViewModel.ResetPasswordAsync();

        if (ViewModel.IssuedCredential is not { } credential)
        {
            return;
        }

        var dialog = new PinRevealDialog(App.GetService<PinRevealDialogViewModel>());
        dialog.ShowFor(credential, XamlRoot);

        await dialog.ShowAsync();

        ViewModel.DismissIssuedCredential();
    }
}
