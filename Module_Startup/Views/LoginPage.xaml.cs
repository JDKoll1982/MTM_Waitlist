using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Startup.ViewModels;

namespace MTM_Waitlist.Module_Startup.Views;

public sealed partial class LoginPage : Page
{
    public LoginViewModel ViewModel
    {
        get;
    }

    public LoginPage()
    {
        ViewModel = App.GetService<LoginViewModel>();
        InitializeComponent();
    }
}
