using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Setup.ViewModels;

namespace MTM_Waitlist.Module_Setup.Views;

public sealed partial class SetupSequenceSelectionPage : Page
{
    public SetupSequenceSelectionViewModel ViewModel { get; }

    public SetupSequenceSelectionPage()
    {
        ViewModel = App.GetService<SetupSequenceSelectionViewModel>();
        InitializeComponent();
    }
}