using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.ViewModels;
using MTM_Waitlist.Module_Shared.Dialogs;

namespace MTM_Waitlist.Module_Setup.Views;

/// <summary>
/// Image-backed Dunnage part search dialog used from the Dunnage &amp; Scrap page.
/// Selecting a card returns the chosen part to the caller, which adds it to the
/// pair's dunnage assignments.
/// </summary>
/// <remarks>
/// <para>
/// The body of a windowed dialog rather than a dialog of its own: it opens in its own window owned by the main one,
/// so it is centred on that window and stays in front of it (014-adopt-lab-design-system, FR-001, FR-002).
/// </para>
/// <para>
/// The host reports a button press after it has already closed, and offers no way to cancel one, so the accept button
/// is disabled until a card is chosen rather than the window refusing to close. The window sizes itself from the shared
/// dialog scale instead of the height and width it used to declare (FR-004).
/// </para>
/// </remarks>
public sealed partial class SetupDunnageImageSearchDialog : UserControl
{
    public SetupDunnageImageSearchDialog(SetupDunnageImageSearchDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = ViewModel;

        MinWidth = DialogSizing.MinWidth;
        Width = DialogSizing.MaxWidth;
        MaxHeight = DialogSizing.MaxBodyHeight;
    }

    public SetupDunnageImageSearchDialogViewModel ViewModel { get; }

    /// <summary>The part chosen by the user, or <c>null</c> when the dialog was dismissed.</summary>
    public SetupDunnagePart? SelectedPart => ViewModel.SelectedPart;

    /// <summary>
    /// Opens the search in its own window, owned by <paramref name="owner"/>.
    /// </summary>
    /// <param name="owner">The application window the search is centred on and belongs to.</param>
    /// <returns>The part chosen, or <c>null</c> when the window was dismissed without one.</returns>
    public async Task<SetupDunnagePart?> ShowWindowedAsync(Window owner)
    {
        var host = WindowedDialogHost.Create(
            owner,
            "Search Dunnage Parts by Image",
            this,
            LocalizeOrDefault("Setup_DunnageSearch.Accept", "Use this part"),
            LocalizeOrDefault("Setup_DunnageSearch.Cancel", "Cancel"),
            MessageDialogButton.Act);

        // Nothing to accept until a card is chosen, and the host cannot refuse to close, so the button carries the rule.
        host.IsPrimaryButtonEnabled = false;
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(SetupDunnageImageSearchDialogViewModel.SelectedPart))
            {
                host.IsPrimaryButtonEnabled = ViewModel.SelectedPart is not null;
            }
        };

        await ViewModel.InitializeAsync();

        var result = await host.ShowAsync();

        return result == ContentDialogResult.Primary ? SelectedPart : null;
    }

    private static string LocalizeOrDefault(string key, string fallback)
    {
        var localized = key.GetLocalized();
        return string.Equals(localized, key, StringComparison.Ordinal) ? fallback : localized;
    }

    private void OnPartClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is SetupDunnagePart part)
        {
            ViewModel.SelectPart(part);
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
