using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist.Module_Core.Helpers;

using MTM_Waitlist.Module_Setup.ViewModels;

namespace MTM_Waitlist.Module_Setup.Views;

public sealed partial class SetupDunnageTypePage : Page
{
    public SetupDunnageTypeViewModel ViewModel { get; }

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

    private async void OnQuickAddTypeClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var inputTextBox = new TextBox
        {
            PlaceholderText = "Enter dunnage type name",
            MaxLength = 100,
        };

        var dialog = new ContentDialog
        {
            Title = "Add New Dunnage Type",
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = inputTextBox,
            XamlRoot = XamlRoot,
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        var addResult = await ViewModel.QuickAddTypeAsync(inputTextBox.Text);
        StartupDebugLog.Info("SetupDunnageTypePage", $"Quick add type dialog completed. Success={addResult.Success}. Message='{addResult.Message}'.");
    }
}