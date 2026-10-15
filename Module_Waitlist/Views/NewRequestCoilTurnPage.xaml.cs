using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

public sealed partial class NewRequestCoilTurnPage : Page
{
    public NewRequestCoilTurnViewModel ViewModel
    {
        get;
    }

    public NewRequestCoilTurnPage()
    {
        ViewModel = App.GetService<NewRequestCoilTurnViewModel>();
        InitializeComponent();
    }
}
