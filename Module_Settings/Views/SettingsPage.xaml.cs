using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel
    {
        get;
    }

    public SettingsPage()
    {
        StartupDebugLog.Info("SettingsPage", "Constructor started.");
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
        StartupDebugLog.Info("SettingsPage", "Constructor completed.");
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = ViewModel.ComputerManagement.LoadAsync();
    }

    private async void RequestItemImages_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new RequestItemImagesDialog(App.GetService<RequestItemImagesDialogViewModel>())
        {
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }

    private async void WorkCenterImages_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WorkCenterImagesDialog(App.GetService<WorkCenterImagesDialogViewModel>())
        {
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }

    /// <summary>Stores the two storage folders and the retention period that are in the boxes.</summary>
    /// <remarks>
    /// Through the view model's command for the same reason the cache folder is: the value that is saved is the one
    /// the command validated, and a test can drive it without a control.
    /// </remarks>
    private void SaveStoragePaths_Click(object sender, RoutedEventArgs e) =>
        ViewModel.SaveStoragePathsCommand.Execute(null);

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

    private async void AddComputer_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.ComputerManagement.CanManageComputers)
        {
            return;
        }

        var editViewModel = App.GetService<ComputerEditDialogViewModel>();
        editViewModel.ConfigureForAdd();

        var dialog = new ComputerEditDialog(editViewModel)
        {
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.ComputerManagement.LoadAsync();
        }
    }

    private async void EditComputer_Click(object sender, RoutedEventArgs e)
    {
        var selected = ViewModel.ComputerManagement.SelectedComputer;
        if (!ViewModel.ComputerManagement.CanManageComputers || selected is null)
        {
            return;
        }

        var editViewModel = App.GetService<ComputerEditDialogViewModel>();
        editViewModel.ConfigureForEdit(selected);

        var dialog = new ComputerEditDialog(editViewModel)
        {
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.ComputerManagement.LoadAsync();
        }
    }
}
