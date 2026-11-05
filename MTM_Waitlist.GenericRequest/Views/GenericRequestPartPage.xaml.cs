using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.GenericRequest.Views;

public sealed partial class GenericRequestPartPage : Page
{
    /// <summary>
    /// How many characters the requester has to type before both plant systems are asked. A read per keystroke would
    /// be a plant-system connection per letter.
    /// </summary>
    private const int MinimumSearchLength = 2;

    public GenericRequestPartPage()
        : this(GenericRequestPageServices.Get<GenericRequestPartViewModel>())
    {
    }

    public GenericRequestPartPage(GenericRequestPartViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadAsync();
    }

    public GenericRequestPartViewModel ViewModel { get; }

    private async void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        if (sender.Text.Length < MinimumSearchLength)
        {
            return;
        }

        await ViewModel.SearchAsync();
    }

    /// <summary>
    /// Put the part number in the box as the requester moves through the suggestions, and nothing more.
    /// </summary>
    /// <remarks>
    /// Choosing a suggestion is navigation, not acceptance: it fires as the requester arrows through the list. An
    /// accepted part is taken on submit, where the control reports the item that was actually picked. Accepting here
    /// would rewrite the number they were part-way through typing and start a sequence read under them mid-word.
    /// </remarks>
    private void OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is GenericRequestPartOption part)
        {
            sender.Text = part.PartNumber;
        }
    }

    private async void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is GenericRequestPartOption chosen)
        {
            await ViewModel.AcceptPartAsync(chosen);
            return;
        }

        await ViewModel.SearchAsync();

        if (!ViewModel.IsPopulated)
        {
            // The read failed, so the frame is already showing the attempt time and a retry. Refusing a number
            // nobody was asked about would blame the requester for the plant system being down.
            return;
        }

        // A typed number is accepted only when a source named exactly that number, which is what "one of the
        // completions" means. Anything else is refused rather than taken as free text (FR-002).
        var typed = sender.Text.Trim();
        var exact = ViewModel.Completions.FirstOrDefault(
            part => string.Equals(part.PartNumber, typed, StringComparison.OrdinalIgnoreCase));

        if (exact is not null)
        {
            await ViewModel.AcceptPartAsync(exact);
            return;
        }

        if (typed.Length > 0)
        {
            ViewModel.RefuseTypedNumber();
        }
    }

    private async void OnSequenceClicked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is GenericRequestSequenceOption sequence)
        {
            await ViewModel.ChooseSequenceAsync(sequence);
        }
    }
}
