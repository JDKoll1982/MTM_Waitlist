using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

/// <summary>
/// The window opened from the part number on a Deliver Component card. It lists where the part is held, the
/// total on hand, and whether the WIP Application or Infor Visual supplied the answer.
/// </summary>
public sealed partial class PartStockDialog : ContentDialog
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
        Title = ViewModel.TitleText;
        CloseButtonText = ViewModel.CloseText;
    }

    /// <summary>
    /// Shows the window for a part. The root a dialog is shown from must be set explicitly in WinUI 3, so it is
    /// set here rather than left to the caller.
    /// </summary>
    /// <param name="partNumber">The part to look up.</param>
    /// <param name="xamlRoot">The root the window is shown from.</param>
    public async Task ShowForAsync(string partNumber, XamlRoot xamlRoot)
    {
        ArgumentNullException.ThrowIfNull(xamlRoot);
        XamlRoot = xamlRoot;

        // The lookup runs while the window is open, so it appears at once with a progress ring.
        _ = LoadAsync(partNumber);
        await ShowAsync();
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
