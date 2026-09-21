using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// The permissions page (FR-063 to FR-074).
/// </summary>
/// <remarks>
/// The page holds no rule of its own: what may be changed, what is locked and what is pending all come from the
/// view model, and the save is confirmed here in the reader's words before anything is written (FR-067).
/// </remarks>
public sealed partial class PermissionsPage : Page
{
    public PermissionsViewModel ViewModel
    {
        get;
    }

    public PermissionsPage()
    {
        StartupDebugLog.Info("PermissionsPage", "Constructor started.");
        ViewModel = App.GetService<PermissionsViewModel>();
        InitializeComponent();
        StartupDebugLog.Info("PermissionsPage", "Constructor completed.");
    }

    private void OnBackClick(object sender, RoutedEventArgs e) => ViewModel.GoBackCommand.Execute(null);

    /// <summary>
    /// Asks what is changing and for whom before it is written, one sentence per changed row and a count when
    /// several change. Nothing is written unless the reader confirms.
    /// </summary>
    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var confirm = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = ViewModel.SaveLabelText,
            Content = string.Join(Environment.NewLine, ViewModel.ConfirmationSentences),
            PrimaryButtonText = ViewModel.SaveLabelText,
            CloseButtonText = ViewModel.BackLabelText,
            DefaultButton = ContentDialogButton.Close,
        };

        if (await confirm.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        await ViewModel.SaveAsync();
    }
}
