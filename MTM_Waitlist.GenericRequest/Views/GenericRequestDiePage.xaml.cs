using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.GenericRequest.Views;

public sealed partial class GenericRequestDiePage : Page
{
    /// <summary>
    /// How many characters the requester has to type before Infor Visual is asked. A read per keystroke would be a
    /// plant-system connection per letter, which is the same rule the part step's search follows.
    /// </summary>
    private const int MinimumSearchLength = 2;

    public GenericRequestDiePage()
        : this(GenericRequestPageServices.Get<GenericRequestDieViewModel>())
    {
    }

    public GenericRequestDiePage(GenericRequestDieViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public GenericRequestDieViewModel ViewModel { get; }

    /// <summary>
    /// Search as the term is typed, which is how the other search fields in this application answer: the dies
    /// narrow as the person types, instead of appearing only when they have typed a whole number and pressed a
    /// button. The step drops a read that lands after a later one, so a fast typist never sees an earlier term's
    /// dies.
    /// </summary>
    private void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason is not AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        if (sender.Text.Trim().Length < MinimumSearchLength)
        {
            return;
        }

        _ = ViewModel.LoadAsync();
    }

    /// <summary>
    /// Mark or unmark the die whose card was clicked, the way the category step's cards choose a category.
    /// </summary>
    /// <remarks>
    /// The click is handled here rather than through the grid's selection because the card carries the choice: the
    /// outline and the tick follow the card's own flag, and clicking a card that is already marked unmarks it, so a
    /// request for several dies is built by tapping the ones it needs (FR-010).
    /// </remarks>
    private void DieGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is GenericRequestDieOption option)
        {
            ViewModel.ToggleDieCommand.Execute(option);
        }
    }
}
