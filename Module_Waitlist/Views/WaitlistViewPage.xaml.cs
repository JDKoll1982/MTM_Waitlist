using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

using System.IO;

using MTM_Waitlist.Module_Core.Contracts.Services;
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

        var localPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Images", "waitlist-empty-state.png");

        if (File.Exists(localPath))
        {
            EmptyStateImage.Source = new BitmapImage(new Uri(localPath, UriKind.Absolute));
            return;
        }

        EmptyStateImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/Images/waitlist-empty-state.png"));
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
