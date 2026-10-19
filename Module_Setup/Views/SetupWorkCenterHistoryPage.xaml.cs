using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Setup.ViewModels;

namespace MTM_Waitlist.Module_Setup.Views;

public sealed partial class SetupWorkCenterHistoryPage : Page
{
    public SetupWorkCenterHistoryViewModel ViewModel { get; }

    public SetupWorkCenterHistoryPage()
    {
        ViewModel = App.GetService<SetupWorkCenterHistoryViewModel>();
        InitializeComponent();
    }
}
