using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// The lugger arrangement (013-lugger-location-config, FR-030, FR-031).
/// </summary>
/// <remarks>
/// <para>
/// The page holds no rule of its own: the cards, the editor's answers, the store's refusal and the unavailable
/// state all come from the view model, which is also where the permission gate is enforced. What the page does
/// own is the two things a window and a drag need and a view model cannot do: opening the modal window that
/// edits one lugger, and telling the store the order a drag left the cards in.
/// </para>
/// <para>
/// <b>Which lugger an action is about is the card's own answer.</b> Each card's Edit and Rename actions hand over
/// the row they were drawn from, so neither window asks the reader to pick a lugger the page could have named.
/// </para>
/// </remarks>
public sealed partial class LuggerArrangementPage : Page
{
    /// <summary>True while a drag's new order is being written, so one drag is persisted once.</summary>
    private bool _isPublishingOrder;

    public LuggerArrangementViewModel ViewModel
    {
        get;
    }

    public LuggerArrangementPage()
    {
        AppLog.Info("LuggerArrangementPage", "Constructor started.");
        ViewModel = App.GetService<LuggerArrangementViewModel>();
        InitializeComponent();
        AppLog.Info("LuggerArrangementPage", "Constructor completed.");
    }

    /// <summary>Opens the editor for the lugger whose card was acted on.</summary>
    private async void OnEditLuggerClick(object sender, RoutedEventArgs e)
    {
        if (RowOf(sender) is not LuggerArrangementRow lugger)
        {
            return;
        }

        ViewModel.SelectLugger(lugger);

        var editor = new LuggerEditorDialog(ViewModel);

        // Its own window, owned by the application's, so it is centred on the window and stays centred when that
        // window is resized.
        await editor.ShowWindowedAsync(lugger.Name, App.GetService<IAppWindowProvider>().MainWindow);
    }

    /// <summary>Asks for the lugger's new name and renames it when the answer is accepted.</summary>
    private async void OnRenameLuggerClick(object sender, RoutedEventArgs e)
    {
        if (RowOf(sender) is not LuggerArrangementRow lugger)
        {
            return;
        }

        ViewModel.BeginRename(lugger);

        await ShowNameDialogAsync(ViewModel.RenameActionText, () => ViewModel.RenameLuggerAsync());
    }

    /// <summary>Asks for a new lugger's name and adds it when the answer is accepted.</summary>
    private async void OnAddLuggerClick(object sender, RoutedEventArgs e)
    {
        ViewModel.BeginAdd();

        await ShowNameDialogAsync(ViewModel.AddActionText, () => ViewModel.AddLuggerAsync());
    }

    /// <summary>
    /// Persists the order a drag left the cards in. The card that moved is the one the store is told about; the
    /// store renumbers the rest, so one call is enough and the page never rewrites the whole list.
    /// </summary>
    private async void OnLuggerDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        if (_isPublishingOrder)
        {
            return;
        }

        var moved = args.Items.OfType<LuggerArrangementRow>().FirstOrDefault();
        if (moved is null)
        {
            return;
        }

        var position = IndexOfCard(sender, moved) + 1;
        if (position <= 0 || position == moved.DisplayPosition)
        {
            // Dropped where it already was, so there is nothing to write and nothing to record.
            return;
        }

        _isPublishingOrder = true;

        try
        {
            await ViewModel.MoveLuggerAsync((moved.PublicId, position));
        }
        finally
        {
            _isPublishingOrder = false;
        }
    }

    private async Task ShowNameDialogAsync(string actionText, Func<Task> accept)
    {
        var dialog = new LuggerNameDialog(ViewModel, actionText, actionText, accept)
        {
            XamlRoot = XamlRoot,
        };

        await dialog.ShowAsync();
    }

    /// <summary>The place a card holds in the grid as it stands, or <c>-1</c> when it is not there.</summary>
    private static int IndexOfCard(ListViewBase list, LuggerArrangementRow card)
    {
        for (var index = 0; index < list.Items.Count; index++)
        {
            if (ReferenceEquals(list.Items[index], card))
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>The lugger a card's own action is about, taken from the card the button was drawn in.</summary>
    private static LuggerArrangementRow? RowOf(object sender) =>
        (sender as FrameworkElement)?.DataContext as LuggerArrangementRow;
}
