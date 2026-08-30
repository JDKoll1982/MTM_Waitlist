using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Views;

// TODO: Set the URL for your privacy policy by updating SettingsPage_PrivacyTermsLink.NavigateUri in Resources.resw.
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

    private async void RequestTypeImages_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new RequestTypeImagesDialog(App.GetService<RequestTypeImagesDialogViewModel>())
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

    private async void RequestSubtypeImages_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new RequestSubtypeImagesDialog(App.GetService<RequestSubtypeImagesDialogViewModel>())
        {
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }

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
