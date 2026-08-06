using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Setup.ViewModels;

namespace MTM_Waitlist.Module_Setup.Views;

public sealed partial class SetupDunnageAddPartSelectionPage : Page
{
    public SetupDunnageAddPartSelectionViewModel ViewModel { get; }

    public SetupDunnageAddPartSelectionPage()
    {
        ViewModel = App.GetService<SetupDunnageAddPartSelectionViewModel>();
        InitializeComponent();
    }
}
