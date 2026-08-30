using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.Views;

namespace MTM_Waitlist.Services;

/// <summary>
/// App-side implementation of <see cref="ISetupDialogService"/> for the Setup
/// dunnage workflow. Lives in the composition root because it needs the app-owned
/// <see cref="SetupDunnageImageSearchDialog"/> view and the live XAML root from the
/// main window.
/// </summary>
public sealed class SetupDialogService : ISetupDialogService
{
    private static string LocalizeOrDefault(string key, string fallback)
    {
        var localized = key.GetLocalized();
        return string.Equals(localized, key, StringComparison.Ordinal) ? fallback : localized;
    }

    /// <inheritdoc />
    public async Task<SetupDunnagePart?> ShowDunnageImageSearchDialogAsync()
    {
        var xamlRoot = (App.MainWindow?.Content as FrameworkElement)?.XamlRoot;
        if (xamlRoot is null)
        {
            // No active XAML root (for example a headless test host); no dialog can be shown.
            return null;
        }

        var dialog = App.GetService<SetupDunnageImageSearchDialog>();
        dialog.XamlRoot = xamlRoot;

        await dialog.ShowAsync();

        return dialog.SelectedPart;
    }

    /// <inheritdoc />
    public async Task<bool> ConfirmNoDunnageAsync()
    {
        var xamlRoot = (App.MainWindow?.Content as FrameworkElement)?.XamlRoot;
        if (xamlRoot is null)
        {
            // No active XAML root (for example a headless test host); proceed.
            return true;
        }

        var dialog = new ContentDialog
        {
            Title = LocalizeOrDefault("Setup_NoDunnage.DialogTitle", "Continue without dunnage?"),
            Content = LocalizeOrDefault("Setup_NoDunnage.DialogMessage", "No dunnage was selected for this job. Do you want to continue without dunnage?"),
            PrimaryButtonText = LocalizeOrDefault("Setup_NoDunnage.Confirm", "Yes"),
            CloseButtonText = LocalizeOrDefault("Setup_NoDunnage.Cancel", "No"),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = xamlRoot,
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}
