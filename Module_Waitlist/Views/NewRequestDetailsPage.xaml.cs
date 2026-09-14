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

    private void OnOptionComboSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OptionCombo.SelectedItem is not string chosen)
        {
            return;
        }

        // Coming Back to this step restores the answer it was entered with, and the restored value arrives here as
        // a selection change. Comparing against the answer the view model recorded on the way in is what tells that
        // apart from a pick — the step must stand so the answer can be corrected. Reading it from the view model
        // rather than caching it in a load handler matters: the binding applies the restored value while the page is
        // still loading, so a cache filled in Loaded would be set after this had already fired once.
        if (string.Equals(chosen, ViewModel.RestoredAnswer, StringComparison.Ordinal))
        {
            return;
        }

        // Written here rather than left to the TwoWay binding, whose update order relative to this event is not
        // something to rely on.
        ViewModel.SelectedOption = chosen;
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
