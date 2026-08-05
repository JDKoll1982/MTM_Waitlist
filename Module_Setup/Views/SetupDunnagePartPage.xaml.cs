using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist.Module_Core.Helpers;

using MTM_Waitlist.Module_Setup.ViewModels;

namespace MTM_Waitlist.Module_Setup.Views;

public sealed partial class SetupDunnagePartPage : Page
{
    public SetupDunnagePartViewModel ViewModel { get; }

    public SetupDunnagePartPage()
    {
        ViewModel = App.GetService<SetupDunnagePartViewModel>();
        InitializeComponent();
    }

    private async void OnQuickAddPartClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var inputTextBox = new TextBox
        {
            PlaceholderText = "Enter dunnage part name",
            MaxLength = 50,
        };

        var dialog = new ContentDialog
        {
            Title = "Add New Dunnage Part",
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

        var addResult = await ViewModel.QuickAddPartAsync(inputTextBox.Text);
        StartupDebugLog.Info("SetupDunnagePartPage", $"Quick add part dialog completed. Success={addResult.Success}. Message='{addResult.Message}'.");
    }
}