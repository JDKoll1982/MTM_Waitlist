using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Shared.Dialogs;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

/// <summary>
/// The window opened from the part number on a Deliver Component card. It lists where the part is held, the
/// total on hand, and whether the WIP Application or Infor Visual supplied the answer.
/// </summary>
/// <remarks>
/// The body of a windowed dialog rather than a dialog of its own: it opens in its own window owned by the main one
/// (014-adopt-lab-design-system, FR-001, FR-002).
/// </remarks>
public sealed partial class PartStockDialog : UserControl
{
    /// <summary>The window's view model.</summary>
    public PartStockDialogViewModel ViewModel
    {
        get;
    }

    /// <summary>Creates the window over its view model.</summary>
    /// <param name="viewModel">The view model that holds the lookup.</param>
    public PartStockDialog(PartStockDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        MinWidth = DialogSizing.MinWidth;
        Width = DialogSizing.PreferredWidth;
        MaxHeight = DialogSizing.MaxBodyHeight;
    }

    /// <summary>
    /// Shows the window for a part, owned by <paramref name="owner"/>.
    /// </summary>
    /// <param name="partNumber">The part to look up.</param>
    /// <param name="owner">The application window the dialog is centred on and belongs to.</param>
    public async Task ShowWindowedAsync(string partNumber, Window owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        var host = WindowedDialogHost.Create(
            owner,
            ViewModel.TitleText,
            this,
            actLabel: null,
            cancelLabel: ViewModel.CloseText,
            MessageDialogButton.Cancel);

        // The lookup runs while the window is open, so it appears at once with a progress ring.
        _ = LoadAsync(partNumber);

        await host.ShowAsync();
    }

    private async Task LoadAsync(string partNumber)
    {
        try
        {
            await ViewModel.LoadAsync(partNumber);
        }
        catch (Exception ex)
        {
            AppLog.Error("PartStockDialog", ex, $"Loading part '{partNumber}' failed.");
        }
    }
}
