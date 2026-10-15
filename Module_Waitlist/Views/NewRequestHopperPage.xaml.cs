using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

public sealed partial class NewRequestHopperPage : Page
{
    public NewRequestHopperViewModel ViewModel
    {
        get;
    }

    public NewRequestHopperPage()
    {
        ViewModel = App.GetService<NewRequestHopperViewModel>();
        InitializeComponent();
    }
}
