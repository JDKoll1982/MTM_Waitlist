using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.ViewModels;

using OpenFilePicker = Windows.Storage.Pickers.FileOpenPicker;
using PickerLocationId = Windows.Storage.Pickers.PickerLocationId;
using PickerViewMode = Windows.Storage.Pickers.PickerViewMode;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// The Pictures page: which picture an item or work centre shows, where the picture files are kept, and the copies
/// kept on this computer.
/// </summary>
public sealed partial class PicturesSettingsPage : Page
{
    public SettingsViewModel ViewModel
    {
        get;
    }

    public PicturesSettingsPage()
    {
        AppLog.Info("PicturesSettingsPage", "Constructor started.");
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
        AppLog.Info("PicturesSettingsPage", "Constructor completed.");
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        SettingsSectionNavigator.BringIntoView(this, e.Parameter);
    }

    private async void RequestItemImages_Click(object sender, RoutedEventArgs e)
    {
        if (App.MainWindow is not { } owner)
        {
            return;
        }

        await new RequestItemImagesDialog(App.GetService<RequestItemImagesDialogViewModel>())
            .ShowWindowedAsync(owner);
    }

    private async void WorkCenterImages_Click(object sender, RoutedEventArgs e)
    {
        if (App.MainWindow is not { } owner)
        {
            return;
        }

        await new WorkCenterImagesDialog(App.GetService<WorkCenterImagesDialogViewModel>())
            .ShowWindowedAsync(owner);
    }

    /// <summary>Stores the two storage folders and the retention period that are in the boxes.</summary>
    /// <remarks>
    /// Through the view model's command for the same reason the cache folder is: the value that is saved is the one
    /// the command validated, and a test can drive it without a control.
    /// </remarks>
    private void SaveStoragePaths_Click(object sender, RoutedEventArgs e) =>
        ViewModel.SaveStoragePathsCommand.Execute(null);

    /// <summary>Chooses the picture the application draws where it has no picture of its own.</summary>
    /// <remarks>
    /// The picker lives here rather than behind a service, as the part-picture screen's does: picking a file is a
    /// platform call that needs the window, and the choice it produces goes into the box, which the Save button
    /// beside it is what stores. Nothing is written by choosing.
    /// </remarks>
    private async void PickNoImage_Click(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFilePicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            ViewMode = PickerViewMode.Thumbnail,
        };

        foreach (var extension in new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" })
        {
            picker.FileTypeFilter.Add(extension);
        }

        // WinUI 3 pickers are unowned until associated with the app window.
        WinRT.Interop.InitializeWithWindow.Initialize(
            picker,
            WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow));

        var file = await picker.PickSingleFileAsync();
        if (!string.IsNullOrWhiteSpace(file?.Path))
        {
            ViewModel.NoImagePathInput = file.Path;
        }
    }

    /// <summary>Stores the picture cache folder that is in the box.</summary>
    /// <remarks>
    /// Through the view model's command rather than the box's own text, so that the value saved is the one the
    /// command validates and the same one a test can drive without a control.
    /// </remarks>
    private void SavePictureCacheFolder_Click(object sender, RoutedEventArgs e) =>
        ViewModel.SavePictureCacheFolderCommand.Execute(null);

    /// <summary>Copies the pictures onto this computer without waiting for the next start.</summary>
    private void RefreshPictureCache_Click(object sender, RoutedEventArgs e) =>
        ViewModel.RefreshPictureCacheCommand.Execute(null);
}
