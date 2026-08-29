using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.ViewModels;

namespace MTM_Waitlist.Module_Setup.Views;

/// <summary>
/// Image-backed Dunnage part search dialog used from the Dunnage &amp; Scrap page.
/// Selecting a card returns the chosen part to the caller, which adds it to the
/// pair's dunnage assignments.
/// </summary>
public sealed partial class SetupDunnageImageSearchDialog : ContentDialog
{
    public SetupDunnageImageSearchDialogViewModel ViewModel { get; }

    public SetupDunnageImageSearchDialog(SetupDunnageImageSearchDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = ViewModel;
    }

    /// <summary>The part chosen by the user, or <c>null</c> when the dialog was dismissed.</summary>
    public SetupDunnagePart? SelectedPart => ViewModel.SelectedPart;

    private async void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        await ViewModel.InitializeAsync();
    }

    private void OnPartClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is SetupDunnagePart part)
        {
            ViewModel.SelectPart(part);
            Hide();
        }
    }

    private async void OnShowPartsWithoutImagesToggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch)
        {
            await ViewModel.HandleShowPartsWithoutImagesChangedAsync(toggleSwitch.IsOn);
        }
    }
}
