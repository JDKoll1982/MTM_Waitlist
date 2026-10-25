using System.ComponentModel;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.ViewModels;
using MTM_Waitlist.Module_Shared.Dialogs;

namespace MTM_Waitlist.Module_Setup.Views;

/// <summary>
/// The work-order search window opened by the magnifier beside the Work Order box. Picking a line hands the
/// chosen order back to the page, which puts its number in the box and lets the page's own search run — the
/// entry page does not learn a second way to look an order up.
/// </summary>
/// <remarks>
/// <para>
/// The body of a windowed dialog rather than a dialog of its own: it opens in its own window owned by the main one
/// (014-adopt-lab-design-system, FR-001, FR-002).
/// </para>
/// <para>
/// The host reports a button press only after it has closed and cannot be asked to stay open, so the accept button is
/// kept disabled until a line is chosen instead of the window refusing the press. Double-clicking a line selects it and
/// the answer is read from the window's own result once it closes, so nothing depends on the order the events arrive in.
/// </para>
/// </remarks>
public sealed partial class SetupWorkOrderSearchDialog : UserControl
{
    private DevWinUI.WindowedContentDialog? _host;

    public SetupWorkOrderSearchDialog(SetupWorkOrderSearchDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = ViewModel;

        MinWidth = DialogSizing.MinWidth;
        Width = DialogSizing.MaxWidth;
        MaxHeight = DialogSizing.MaxBodyHeight;
    }

    public SetupWorkOrderSearchDialogViewModel ViewModel { get; }

    /// <summary>The order the operator accepted, or <c>null</c> when the window was dismissed.</summary>
    public SetupWorkOrderSearchResult? SelectedWorkOrder => ViewModel.AcceptedResult;

    /// <summary>
    /// Opens the search in its own window, owned by <paramref name="owner"/>.
    /// </summary>
    /// <param name="owner">The application window the search is centred on and belongs to.</param>
    /// <returns>The order accepted, or <c>null</c> when the window was dismissed.</returns>
    public async Task<SetupWorkOrderSearchResult?> ShowWindowedAsync(Window owner)
    {
        _host = WindowedDialogHost.Create(
            owner,
            ViewModel.TitleText,
            this,
            ViewModel.AcceptText,
            ViewModel.CancelText,
            MessageDialogButton.Act);

        _host.IsPrimaryButtonEnabled = ViewModel.HasSelection;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        try
        {
            await ViewModel.InitializeAsync();

            var result = await _host.ShowAsync();

            if (result == ContentDialogResult.Primary && ViewModel.SelectedResult is { } chosen)
            {
                ErrorText.Visibility = Visibility.Collapsed;
                ViewModel.Accept(chosen);
            }
        }
        finally
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            ViewModel.MarkClosed();
            _host = null;
        }

        return SelectedWorkOrder;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SetupWorkOrderSearchDialogViewModel.HasSelection) && _host is { } host)
        {
            host.IsPrimaryButtonEnabled = ViewModel.HasSelection;
        }
    }

    private void OnResultDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (FindRow(e.OriginalSource as DependencyObject) is not { } row)
        {
            return;
        }

        ViewModel.Accept(row);
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
