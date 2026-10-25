using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// The Devices page: the registry of the computers that may run MTM Waitlist.
/// </summary>
public sealed partial class DevicesSettingsPage : Page
{
    public SettingsViewModel ViewModel
    {
        get;
    }

    public DevicesSettingsPage()
    {
        AppLog.Info("DevicesSettingsPage", "Constructor started.");
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
        AppLog.Info("DevicesSettingsPage", "Constructor completed.");
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = ViewModel.ComputerManagement.LoadAsync();
        SettingsSectionNavigator.BringIntoView(this, e.Parameter);
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