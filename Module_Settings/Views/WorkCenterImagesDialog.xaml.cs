using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Views;

public sealed partial class WorkCenterImagesDialog : ContentDialog
{
    private readonly WorkCenterImagesDialogViewModel _viewModel;

    public WorkCenterImagesDialog(WorkCenterImagesDialogViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        Editor.Attach(_viewModel);

        Opened += OnOpened;
        PrimaryButtonClick += OnPrimaryButtonClick;
        CloseButtonClick += OnCloseButtonClick;
        Closed += OnClosed;
    }

    private async void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        await _viewModel.LoadAsync();

        // Save stays disabled while the catalog is unavailable.
        IsPrimaryButtonEnabled = _viewModel.CanSave;

        if (_viewModel.HasOrphanedOverrides)
        {
            await PromptToPruneOrphansAsync();
        }
    }

    private async Task PromptToPruneOrphansAsync()
    {
        var confirm = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Remove orphaned overrides?",
            Content = $"{_viewModel.OrphanedItemIds.Count} override(s) point at work centers that no longer exist in the catalog. Removing them keeps the list clean.",
            PrimaryButtonText = "Remove",
            CloseButtonText = "Keep",
            DefaultButton = ContentDialogButton.Close
        };

        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
        {
            await _viewModel.PruneOrphanedOverridesAsync();
        }
    }

    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            args.Cancel = !await _viewModel.SaveAsync();
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void OnCloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) =>
        _viewModel.CancelEdits();

    private void OnClosed(ContentDialog sender, ContentDialogClosedEventArgs args) => Editor.Detach();
}
