using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Startup.ViewModels;

namespace MTM_Waitlist.Module_Startup.Views;

public sealed partial class SplashPage : Page
{
    public SplashViewModel ViewModel { get; }

    public SplashPage()
    {
        ViewModel = App.GetService<SplashViewModel>();
        InitializeComponent();
    }
}
