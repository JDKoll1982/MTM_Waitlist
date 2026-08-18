using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
}
