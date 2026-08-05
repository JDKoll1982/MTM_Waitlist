using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Setup.ViewModels;

namespace MTM_Waitlist.Module_Setup.Views;

public sealed partial class SetupPartSelectionPage : Page
{
    public SetupPartSelectionViewModel ViewModel { get; }

    public SetupPartSelectionPage()
    {
        ViewModel = App.GetService<SetupPartSelectionViewModel>();
        InitializeComponent();
    }
}