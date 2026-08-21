using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

public sealed partial class NewRequestResultPage : Page
{
    public NewRequestResultViewModel ViewModel
    {
        get;
    }

    public NewRequestResultPage()
    {
        ViewModel = App.GetService<NewRequestResultViewModel>();
        InitializeComponent();
    }
}
