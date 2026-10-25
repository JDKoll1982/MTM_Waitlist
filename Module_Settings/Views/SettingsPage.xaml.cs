using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// The settings hub. It owns no setting: each card opens the page that does, so the handlers that used to live here
/// moved with the markup they belong to.
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel
    {
        get;
    }

    public SettingsPage()
    {
        AppLog.Info("SettingsPage", "Constructor started.");
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
        AppLog.Info("SettingsPage", "Constructor completed.");
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // The Devices card is drawn from the registry's own answer, so the hub loads it like the Devices page does.
        _ = ViewModel.ComputerManagement.LoadAsync();
    }

    /// <summary>Fills the suggestion list as the reader types.</summary>
    /// <remarks>
    /// Only user input is answered: setting the box's text from a chosen suggestion also raises this event, and
    /// re-filtering on it would replace the list the reader is looking at with a single entry.
    /// </remarks>
    private void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        ViewModel.PageSearchQuery = sender.Text;
        ViewModel.UpdatePageSearch(sender.Text);
        sender.ItemsSource = ViewModel.PageSearchSuggestions;
    }

    /// <summary>Puts the chosen suggestion's own name in the box.</summary>
    private void OnSearchSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is SettingsSearchTarget target)
        {
            sender.Text = target.Title;
        }
    }

    /// <summary>
    /// Opens the setting the reader settled on.
    /// </summary>
    /// <remarks>
    /// Enter without picking a suggestion still works: the first match is taken, so the search does not require a
    /// second gesture to be useful.
    /// </remarks>
    private void OnSearchQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var target = args.ChosenSuggestion as SettingsSearchTarget ?? ViewModel.FindPageSearch(args.QueryText);

        if (target is null)
        {
            return;
        }

        ViewModel.OpenSearchTargetCommand.Execute(target);
    }
}