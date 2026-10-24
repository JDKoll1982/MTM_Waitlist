using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Views;

public sealed partial class GeneralSettingsPage : Page
{
    public SettingsViewModel ViewModel
    {
        get;
    }

    public GeneralSettingsPage()
    {
        AppLog.Info("GeneralSettingsPage", "Constructor started.");
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
        AppLog.Info("GeneralSettingsPage", "Constructor completed.");
    }
}
