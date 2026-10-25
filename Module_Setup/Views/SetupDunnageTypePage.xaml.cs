using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist.Module_Core.Helpers;

using MTM_Waitlist.Module_Setup.ViewModels;
using MTM_Waitlist.Module_Shared.Dialogs;

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
            AppLog.Info("SetupDunnageTypePage", "Page constructor started.");
            ViewModel = App.GetService<SetupDunnageTypeViewModel>();
            InitializeComponent();
            AppLog.Info("SetupDunnageTypePage", "Page constructor completed.");
        }
        catch (Exception ex)
        {
            AppLog.Error("SetupDunnageTypePage", ex, "Page constructor failed.");
            throw;
        }
    }

    private async void OnClearAllForPairClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (App.MainWindow is not { } owner)
        {
            return;
        }

        var outcome = await MessageDialogHost.AskAsync(owner, new MessageDialogRequest
        {
            Kind = MessageKind.DestructiveConfirm,
            Title = LocalizeOrDefault("Setup_DunnagePair_ClearAll.DialogTitle", "Clear all dunnage assignments?"),
            Message = LocalizeOrDefault("Setup_DunnagePair_ClearAll.DialogMessage", "This will remove all dunnage assignments for the current part/sequence pair."),
            ActLabel = LocalizeOrDefault("Setup_DunnagePair_ClearAll.Confirm", "Clear All"),
            CancelLabel = LocalizeOrDefault("Setup_DunnagePair_ClearAll.Cancel", "Cancel"),
        });

        if (outcome.IsAct && ViewModel.ClearAllForPairCommand.CanExecute(null))
        {
            ViewModel.ClearAllForPairCommand.Execute(null);
        }
    }

    private async void OnAddScrapTypeClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        AppLog.Info("SetupDunnageTypePage", "OnAddScrapTypeClick started. Opening add-scrap dialog.");
        var scrapTypeInput = new TextBox
        {
            PlaceholderText = LocalizeOrDefault("Setup_DunnagePair.ScrapType.DialogPlaceholder", "Enter scrap type"),
            MinWidth = 320
        };

        if (App.MainWindow is not { } owner)
        {
            AppLog.Info("SetupDunnageTypePage", "No window owns the add-scrap dialog, so nothing was added.");
            return;
        }

        // Collects a value rather than asking a question, so it keeps its own body and takes only the shared window.
        var result = await WindowedDialogHost.ShowAsync(
            owner,
            LocalizeOrDefault("Setup_DunnagePair.ScrapType.DialogTitle", "Add Scrap Type"),
            scrapTypeInput,
            LocalizeOrDefault("Setup_DunnagePair.ScrapType.Add", "Add"),
            LocalizeOrDefault("Setup_DunnagePair.ScrapType.Cancel", "Cancel"),
            MessageDialogButton.Act);

        AppLog.Info("SetupDunnageTypePage", $"Add-scrap dialog completed. Result='{result}', Input='{scrapTypeInput.Text}'.");
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.AddScrapType(scrapTypeInput.Text);
            AppLog.Info("SetupDunnageTypePage", "Primary action selected. ViewModel.AddScrapType invoked.");
        }
        else
        {
            AppLog.Info("SetupDunnageTypePage", "Add-scrap dialog canceled/closed without adding a value.");
        }
    }

}