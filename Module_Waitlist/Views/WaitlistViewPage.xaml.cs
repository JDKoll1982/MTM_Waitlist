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

    private void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (ViewModel.ItemClickCommand != null && ViewModel.ItemClickCommand.CanExecute(e.ClickedItem))
        {
            ViewModel.ItemClickCommand.Execute(e.ClickedItem);
        }
    }

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
