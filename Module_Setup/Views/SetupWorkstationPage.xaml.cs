using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Setup.ViewModels;

namespace MTM_Waitlist.Module_Setup.Views;

public sealed partial class SetupWorkstationPage : Page
{
    public SetupWorkstationViewModel ViewModel { get; }

    public SetupWorkstationPage()
    {
        ViewModel = App.GetService<SetupWorkstationViewModel>();
        InitializeComponent();
    }
}
