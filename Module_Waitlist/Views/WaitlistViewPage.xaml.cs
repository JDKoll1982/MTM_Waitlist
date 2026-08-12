using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

using System.IO;

using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Waitlist.Services;
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

    private async void OnAddRequestClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var workCenterDialogService = App.GetService<IWorkCenterSelectionDialogService>();
        var newRequestDialogService = App.GetService<IWaitlistNewRequestDialogService>();
        if (XamlRoot is null)
        {
            return;
        }

        while (true)
        {
            var selectedWorkCenter = await workCenterDialogService.ShowForCurrentWorkstationAsync(XamlRoot).ConfigureAwait(true);
            if (string.IsNullOrWhiteSpace(selectedWorkCenter))
            {
                return;
            }

            var requestType = await newRequestDialogService.ShowJobTypeSelectionAsync(XamlRoot, selectedWorkCenter).ConfigureAwait(true);
            if (!string.IsNullOrWhiteSpace(requestType))
            {
                return;
            }

            var retryDialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "Restart workflow",
                Content = new TextBlock
                {
                    Text = "Choose a different work center or cancel to return to the waitlist.",
                    TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                },
                PrimaryButtonText = "Try again",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
            };

            var result = await retryDialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }
        }
    }
}
