using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist.Module_Core.Helpers;

using MTM_Waitlist.Module_Setup.ViewModels;

namespace MTM_Waitlist.Module_Setup.Views;

public sealed partial class SetupDunnageTypePage : Page
{
    public SetupDunnageTypeViewModel ViewModel { get; }

    private static string LocalizeOrDefault(string key, string fallback)
    {
        var localized = key.GetLocalized();
        return string.Equals(localized, key, StringComparison.Ordinal) ? fallback : localized;
    }

    public SetupDunnageTypePage()
    {
        try
        {
            StartupDebugLog.Info("SetupDunnageTypePage", "Page constructor started.");
            ViewModel = App.GetService<SetupDunnageTypeViewModel>();
            InitializeComponent();
            StartupDebugLog.Info("SetupDunnageTypePage", "Page constructor completed.");
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SetupDunnageTypePage", ex, "Page constructor failed.");
            throw;
        }
    }

    private async void OnClearAllForPairClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = LocalizeOrDefault("Setup_DunnagePair.ClearAll.DialogTitle", "Clear all dunnage assignments?"),
            Content = LocalizeOrDefault("Setup_DunnagePair.ClearAll.DialogMessage", "This will remove all dunnage assignments for the current part/sequence pair."),
            PrimaryButtonText = LocalizeOrDefault("Setup_DunnagePair.ClearAll.Confirm", "Clear All"),
            CloseButtonText = LocalizeOrDefault("Setup_DunnagePair.ClearAll.Cancel", "Cancel"),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot,
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && ViewModel.ClearAllForPairCommand.CanExecute(null))
        {
            ViewModel.ClearAllForPairCommand.Execute(null);
        }
    }

}