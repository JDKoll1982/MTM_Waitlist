using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Module_Shared.Dialogs;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// The Work-Center-keyed picture screen, shown in its own window owned by the main one
/// (014-adopt-lab-design-system, FR-001).
/// </summary>
/// <remarks>
/// The windowed host reports its button press after it has closed and cannot refuse one, so the orphan question is
/// asked before the window opens and a failed save reports itself afterwards. The edits stay in the view model, so
/// nothing typed is lost and the screen is one press away.
/// </remarks>
public sealed partial class WorkCenterImagesDialog : UserControl
{
    private readonly WorkCenterImagesDialogViewModel _viewModel;

    public WorkCenterImagesDialog(WorkCenterImagesDialogViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        Editor.Attach(_viewModel);

        MinWidth = DialogSizing.MinWidth;
        Width = DialogSizing.MaxWidth;
        MaxHeight = DialogSizing.MaxBodyHeight;
    }

    /// <summary>
    /// Opens the screen in its own window, owned by <paramref name="owner"/>, loads it and saves what was chosen.
    /// </summary>
    /// <param name="owner">The application window the screen is centred on and belongs to.</param>
    /// <returns><see langword="true"/> when the chosen pictures were saved.</returns>
    public async Task<bool> ShowWindowedAsync(Window owner)
    {
        await _viewModel.LoadAsync();

        if (_viewModel.HasOrphanedOverrides)
        {
            await PromptToPruneOrphansAsync(owner);
        }

        var host = WindowedDialogHost.Create(
            owner,
            "Work Center Images",
            this,
            "Save",
            "Cancel",
            MessageDialogButton.Act);

        // Save stays disabled while the catalog is unavailable.
        host.IsPrimaryButtonEnabled = _viewModel.CanSave;

        var result = await host.ShowAsync();

        try
        {
            if (result != ContentDialogResult.Primary)
            {
                _viewModel.CancelEdits();
                return false;
            }

            if (await _viewModel.SaveAsync())
            {
                return true;
            }

            await ReportFailureAsync(owner);
            return false;
        }
        finally
        {
            Editor.Detach();
        }
    }

    private async Task PromptToPruneOrphansAsync(Window owner)
    {
        var outcome = await MessageDialogHost.AskAsync(owner, new MessageDialogRequest
        {
            Kind = MessageKind.DestructiveConfirm,
            Title = "Remove orphaned overrides?",
            Message = $"{_viewModel.OrphanedItemIds.Count} override(s) point at work centers that no longer exist in the catalog. Removing them keeps the list clean.",
            ActLabel = "Remove",
            CancelLabel = "Keep",
        });

        if (outcome.IsAct)
        {
            await _viewModel.PruneOrphanedOverridesAsync();
        }
    }

    private async Task ReportFailureAsync(Window owner)
    {
        var message = string.IsNullOrWhiteSpace(_viewModel.ErrorMessage)
            ? "The pictures could not be saved. Nothing was written."
            : _viewModel.ErrorMessage;

        var outcome = await MessageDialogHost.AskAsync(owner, new MessageDialogRequest
        {
            Kind = MessageKind.Retry,
            Title = _viewModel.Title,
            Message = message,
            ActLabel = "Try again",
            CancelLabel = "Close",
        });

        if (outcome.IsAct)
        {
            // Reopens the same screen over the edits the view model still holds.
            await ShowWindowedAsync(owner);
        }
    }
}
