using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using MTM_Waitlist.Module_Waitlist.ViewModels;

using Windows.System;

namespace MTM_Waitlist.Module_Waitlist.Views;

/// <summary>
/// Additional-details step of the New Request wizard. The step has no Continue button: the answer itself is the
/// way out. A listed answer moves the flow on as soon as it is picked, and a typed answer moves it on when Enter
/// is pressed — so a value that does not satisfy the Item's configuration row is reported on the step the person
/// is still looking at, rather than ending the request somewhere else (FR-014, FR-026).
/// </summary>
public sealed partial class NewRequestDetailsPage : Page
{
    public NewRequestDetailsViewModel ViewModel
    {
        get;
    }

    public NewRequestDetailsPage()
    {
        ViewModel = App.GetService<NewRequestDetailsViewModel>();
        InitializeComponent();
    }

    /// <summary>
    /// Chooses the answer whose card was clicked, and moves the flow on exactly as a drop-down selection did.
    /// </summary>
    private void OnOptionCardClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not AnswerOptionCard card)
        {
            return;
        }

        // Coming Back to this step restores the answer it was entered with, and that restore is drawn as the card
        // already chosen. Comparing against the answer the view model recorded on the way in is what tells that
        // apart from a pick — the step must stand so the answer can be corrected.
        if (string.Equals(card.Answer, ViewModel.RestoredAnswer, StringComparison.Ordinal))
        {
            return;
        }

        ViewModel.SelectAnswer(card.Answer);
        ViewModel.ContinueCommand.Execute(null);
    }

    private void OnDetailInputKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
        {
            return;
        }

        // Enter is the advance, so it must not also be a line break in the answer.
        e.Handled = true;

        // {x:Bind} sends TextBox.Text to its source on lost focus and Enter does not move focus: without this the
        // step would judge the answer it was entered with instead of the one just typed.
        ViewModel.InputValue = DetailInput.Text;
        ViewModel.ContinueCommand.Execute(null);
    }
}
