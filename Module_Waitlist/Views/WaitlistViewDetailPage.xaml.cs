using CommunityToolkit.WinUI.UI.Animations;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

public sealed partial class WaitlistViewDetailPage : Page
{
    public WaitlistViewDetailViewModel ViewModel
    {
        get;
    }

    public WaitlistViewDetailPage()
    {
        ViewModel = App.GetService<WaitlistViewDetailViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        this.RegisterElementForConnectedAnimation("animationKeyContentGrid", ItemHero);
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);
        if (e.NavigationMode == NavigationMode.Back)
        {
            var navigationService = App.GetService<INavigationService>();

            if (ViewModel.Item != null)
            {
                navigationService.SetListDataItemForNextConnectedAnimation(ViewModel.Item);
            }
        }
    }

    private void OpenImageViewer_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is Image image && image.Source is not null)
        {
            ImageViewerImage.Source = image.Source;
            ImageViewerOverlay.Visibility = Visibility.Visible;
        }
    }

    private void ImageViewerClose_Click(object sender, RoutedEventArgs e)
    {
        ImageViewerOverlay.Visibility = Visibility.Collapsed;
        ImageViewerImage.Source = null;
    }

    private void ImageViewerScrim_Tapped(object sender, TappedRoutedEventArgs e)
    {
        ImageViewerOverlay.Visibility = Visibility.Collapsed;
        ImageViewerImage.Source = null;
    }
}
