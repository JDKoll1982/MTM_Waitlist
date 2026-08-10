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

    private async void OnAddScrapTypeClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        StartupDebugLog.Info("SetupDunnageTypePage", "OnAddScrapTypeClick started. Opening add-scrap dialog.");
        var scrapTypeInput = new TextBox
        {
            PlaceholderText = LocalizeOrDefault("Setup_DunnagePair.ScrapType.DialogPlaceholder", "Enter scrap type"),
            MinWidth = 320
        };

        var dialog = new ContentDialog
        {
            Title = LocalizeOrDefault("Setup_DunnagePair.ScrapType.DialogTitle", "Add Scrap Type"),
            Content = scrapTypeInput,
            PrimaryButtonText = LocalizeOrDefault("Setup_DunnagePair.ScrapType.Add", "Add"),
            CloseButtonText = LocalizeOrDefault("Setup_DunnagePair.ScrapType.Cancel", "Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };

        var result = await dialog.ShowAsync();
        StartupDebugLog.Info("SetupDunnageTypePage", $"Add-scrap dialog completed. Result='{result}', Input='{scrapTypeInput.Text}'.");
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.AddScrapType(scrapTypeInput.Text);
            StartupDebugLog.Info("SetupDunnageTypePage", "Primary action selected. ViewModel.AddScrapType invoked.");
        }
        else
        {
            StartupDebugLog.Info("SetupDunnageTypePage", "Add-scrap dialog canceled/closed without adding a value.");
        }
    }

}