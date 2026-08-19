using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

using System.IO;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Module_Waitlist.Models;

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

            var draft = await newRequestDialogService.ShowJobTypeSelectionAsync(XamlRoot, ViewModel.SelectedBuilding, selectedWorkCenter).ConfigureAwait(true);
            if (draft is null)
            {
                return;
            }

            var requestService = App.GetService<IWaitlistRequestService>();
            var submitResult = await requestService.SubmitAsync(draft, allowDuplicate: false).ConfigureAwait(true);
            if (submitResult.Status == WaitlistRequestSubmitStatus.DuplicateWarningRequired)
            {
                var duplicateDialog = new ContentDialog
                {
                    XamlRoot = XamlRoot,
                    Title = "Matching request already active",
                    Content = new TextBlock { Text = submitResult.Message, TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap },
                    PrimaryButtonText = "Continue",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                };
                RequestDialogStyling.Apply(duplicateDialog);
                if (await duplicateDialog.ShowAsync() != ContentDialogResult.Primary)
                {
                    return;
                }

                submitResult = await requestService.SubmitAsync(draft, allowDuplicate: true).ConfigureAwait(true);
            }

            if (submitResult.Status == WaitlistRequestSubmitStatus.Success)
            {
                StartupDebugLog.Info("WaitlistRequest", $"Submission succeeded. Refreshing building '{ViewModel.SelectedBuilding}'.");
                await ViewModel.RefreshAsync();

                var completedResult = await ShowSubmissionCompleteDialogAsync().ConfigureAwait(true);
                if (completedResult)
                {
                    return;
                }

                continue;
            }

            var retryResult = await ShowSubmissionFailureDialogAsync(submitResult.Message).ConfigureAwait(true);
            if (retryResult)
            {
                continue;
            }

            return;
        }
    }

    private static async Task<bool> ShowSubmissionCompleteDialogAsync()
    {
        var dialog = new ContentDialog
        {
            XamlRoot = App.MainWindow?.Content?.XamlRoot,
            Title = "Request completed",
            Content = new TextBlock
            {
                Text = "Your request has been created. Choose whether to return to the waitlist or start another request.",
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
            },
            PrimaryButtonText = "Return to Waitlist",
            SecondaryButtonText = "Add Another Request",
            DefaultButton = ContentDialogButton.Primary,
        };

        RequestDialogStyling.Apply(dialog);

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private static async Task<bool> ShowSubmissionFailureDialogAsync(string message)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = App.MainWindow?.Content?.XamlRoot,
            Title = "Request not submitted",
            Content = new TextBlock
            {
                Text = message,
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
            },
            PrimaryButtonText = "Retry",
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Primary,
        };

        RequestDialogStyling.Apply(dialog);

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}
