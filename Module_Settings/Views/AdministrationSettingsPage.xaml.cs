using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Views;

public sealed partial class AdministrationSettingsPage : Page
{
    public SettingsViewModel ViewModel
    {
        get;
    }

    public AdministrationSettingsPage()
    {
        AppLog.Info("AdministrationSettingsPage", "Constructor started.");
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
        AppLog.Info("AdministrationSettingsPage", "Constructor completed.");
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        SettingsSectionNavigator.BringIntoView(this, e.Parameter);
    }
}
