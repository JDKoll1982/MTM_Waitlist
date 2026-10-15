using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

public sealed partial class WaitlistViewPage : Page
{
    public WaitlistViewViewModel ViewModel
    {
        get;
    }

    public WaitlistViewPage()
    {
        ViewModel = App.GetService<WaitlistViewViewModel>();
        InitializeComponent();
        Loaded += WaitlistViewPage_Loaded;
    }

    private void WaitlistViewPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        SetEmptyStateImageSource();
    }

    private void SetEmptyStateImageSource()
    {
        if (EmptyStateImage is null)
        {
            return;
        }

        // The illustration is the app's own artwork, not a configured picture: it is drawn when it ships, and
        // falls back to the no-image placeholder only if the asset is missing from the deployment. The folder
        // this used to look in first (Assets/Images) has never existed in this repository.
        EmptyStateImage.Source = PictureSource.FromPath("Assets/Placeholders/waitlist-empty-state.png");
    }

    /// <summary>
    /// The condition a row's own material handler chose from its status word: the row's command stores it, so the
    /// one write path and its affected-row reporting are the same as every other action on the list, and a lost
    /// race is told rather than shown as success (FR-070, FR-072, FR-076).
    /// </summary>
    private void OnCardStatusConditionChosen(object? sender, (SampleOrder Row, string Condition) selection) =>
        ViewModel.SetStatusConditionCommand.Execute((selection.Row, selection.Condition));

    // The card body no longer opens the request: the card's own `Open details` button is the one path that
    // does, through the navigation service. The list keeps its row selection so a Shift+click on the picture
    // is still the gesture that enlarges it, and nothing acts on the selection itself (FR-003, FR-068, FR-069).
    private void OnAddRequestClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var navigationService = App.GetService<INavigationService>();
        var state = new NewRequestFlowState
        {
            Building = ViewModel.SelectedBuilding,
        };
        navigationService.NavigateTo(typeof(NewRequestWorkCenterViewModel).FullName!, state);
    }
}
