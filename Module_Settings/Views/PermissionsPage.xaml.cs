using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// The permissions page (FR-063 to FR-074).
/// </summary>
/// <remarks>
/// The page holds no rule of its own: what may be changed, what is locked and what is pending all come from the
/// view model, and a save is confirmed here in the reader's words before anything is written (FR-067).
/// </remarks>
public sealed partial class PermissionsPage : Page
{
    /// <summary>
    /// Whether the confirmation is already on screen. Only one ContentDialog can be open at a time, and a second
    /// <c>ShowAsync</c> while one is up does not close the first: it throws, and because this handler is
    /// <c>async void</c> the exception takes the application down with it. A double press of Save is one press.
    /// </summary>
    private bool _isConfirming;

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

    /// <summary>A person's name opens their own page, which is where their account is changed (FR-077).</summary>
    private void OnPersonLinkClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: PermissionMatrixRow person })
        {
            ViewModel.OpenPersonCommand.Execute(person);
        }
    }

    /// <summary>
    /// One person's unsaved changes, written in one call once the reader has confirmed what changes and for whom.
    /// The sentences name the person and each cell, so the reader is never asked a bare "Save changes?" (FR-067).
    /// </summary>
    private async void OnSaveRowClick(object sender, RoutedEventArgs e)
    {
        if (_isConfirming || sender is not FrameworkElement { Tag: PermissionMatrixRow person })
        {
            return;
        }

        _isConfirming = true;

        try
        {
            var confirm = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = ViewModel.ConfirmTitleText,
                Content = string.Join(Environment.NewLine, person.ConfirmationSentences),
                PrimaryButtonText = ViewModel.SaveLabelText,
                CloseButtonText = ViewModel.CancelLabelText,
                DefaultButton = ContentDialogButton.Close,
            };

            if (await confirm.ShowAsync() != ContentDialogResult.Primary)
            {
                return;
            }

            await ViewModel.SaveAsync(person);
        }
        finally
        {
            _isConfirming = false;
        }
    }
}
