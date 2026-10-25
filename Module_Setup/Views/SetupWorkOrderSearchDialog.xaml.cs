using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.ViewModels;

namespace MTM_Waitlist.Module_Setup.Views;

/// <summary>
/// The work-order search window opened by the magnifier beside the Work Order box. Picking a line hands the
/// chosen order back to the page, which puts its number in the box and lets the page's own search run — the
/// entry page does not learn a second way to look an order up.
/// </summary>
public sealed partial class SetupWorkOrderSearchDialog : ContentDialog
{
    public SetupWorkOrderSearchDialogViewModel ViewModel { get; }

    public SetupWorkOrderSearchDialog(SetupWorkOrderSearchDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = ViewModel;
    }

    /// <summary>The order the operator accepted, or <c>null</c> when the window was dismissed.</summary>
    public SetupWorkOrderSearchResult? SelectedWorkOrder => ViewModel.AcceptedResult;

    private async void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        await ViewModel.InitializeAsync();
    }

    private void OnClosing(ContentDialog sender, ContentDialogClosingEventArgs args) => ViewModel.MarkClosed();

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (ViewModel.SelectedResult is null)
        {
            // Nothing chosen: keep the window open and say what is missing rather than closing with no order.
            args.Cancel = true;
            ErrorText.Text = ViewModel.SelectionRequiredMessage;
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        ErrorText.Visibility = Visibility.Collapsed;
        ViewModel.Accept(ViewModel.SelectedResult);
    }

    private void OnResultDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (FindRow(e.OriginalSource as DependencyObject) is not { } row)
        {
            return;
        }

        ViewModel.Accept(row);
        Hide();
    }

    private void OnFilterKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != Windows.System.VirtualKey.Enter || !ViewModel.IsIdle)
        {
            return;
        }

        e.Handled = true;
        _ = ViewModel.SearchCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Finds the result row a double-click landed on. A double-click inside a row lands on the text, not on the
    /// list item, so the visual tree is walked up to the container that holds the row's own data.
    /// </summary>
    private static SetupWorkOrderSearchResult? FindRow(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is ListViewItem { Content: SetupWorkOrderSearchResult row })
            {
                return row;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }
}
