using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Module_Shared.Dialogs;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// The Item-keyed picture screen. Its code-behind constructs
/// <see cref="RequestItemImagesDialogViewModel"/>, which is what makes the rename and the re-pointing one
/// change rather than two.
/// </summary>
/// <remarks>
/// The windowed host reports its button press after it has closed and cannot refuse one, so a save that fails reports
/// itself through the shared message window afterwards rather than holding this one open. The edits stay in the view
/// model, so the editor is one press away and nothing typed is lost (014-adopt-lab-design-system, FR-001, FR-027).
/// </remarks>
public sealed partial class RequestItemImagesDialog : UserControl
{
    private readonly RequestItemImagesDialogViewModel _viewModel;

    public RequestItemImagesDialog(RequestItemImagesDialogViewModel viewModel)
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

        var host = WindowedDialogHost.Create(
            owner,
            _viewModel.Title,
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

    private async Task ReportFailureAsync(Window owner)
    {
        var message = string.IsNullOrWhiteSpace(_viewModel.ErrorMessage)
            ? "The pictures could not be saved. Nothing was written."
            : _viewModel.ErrorMessage;

        await MessageDialogHost.AskAsync(owner, new MessageDialogRequest
        {
            Kind = MessageKind.Retry,
            Title = "Request Item Images",
            Message = message,
            ActLabel = "Try again",
            CancelLabel = "Close",
        });

        // "Try again" reopens the same screen over the edits the view model still holds.
        await ShowWindowedAsync(owner);
    }
}
