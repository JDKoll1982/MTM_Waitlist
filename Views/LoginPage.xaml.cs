using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.ViewModels;

namespace MTM_Waitlist.Views;

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
