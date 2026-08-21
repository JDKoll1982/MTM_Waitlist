using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

public sealed partial class NewRequestDetailsPage : Page
{
    public NewRequestDetailsViewModel ViewModel
    {
        get;
    }

    public NewRequestDetailsPage()
    {
        ViewModel = App.GetService<NewRequestDetailsViewModel>();
        InitializeComponent();
    }
}
