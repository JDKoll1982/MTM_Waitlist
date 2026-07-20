using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.ViewModels;

namespace MTM_Waitlist.Views;

public sealed partial class SplashPage : Page
{
    public SplashViewModel ViewModel { get; }

    public SplashPage()
    {
        ViewModel = App.GetService<SplashViewModel>();
        InitializeComponent();
    }
}
