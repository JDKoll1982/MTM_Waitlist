using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// The Diagnostics page: the developer log, how much of it is kept, and the cache refresh.
/// </summary>
public sealed partial class DiagnosticsSettingsPage : Page
{
    public SettingsViewModel ViewModel
    {
        get;
    }

    public DiagnosticsSettingsPage()
    {
        AppLog.Info("DiagnosticsSettingsPage", "Constructor started.");
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
        AppLog.Info("DiagnosticsSettingsPage", "Constructor completed.");
    }

    /// <summary>Stores how much the log store keeps, for every computer.</summary>
    /// <remarks>
    /// Through the view model's command for the same reason the cache folder is: the value that is saved is the one
    /// the command validated and adopted, and a test can drive it without a control.
    /// </remarks>
    private void SaveLogStoreLevel_Click(object sender, RoutedEventArgs e) =>
        ViewModel.SaveLogStoreLevelCommand.Execute(null);
}