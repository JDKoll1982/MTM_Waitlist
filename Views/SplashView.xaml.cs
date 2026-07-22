using MTM_Waitlist.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace MTM_Waitlist.Views;

public sealed partial class SplashView : UserControl
{
    public SplashViewModel ViewModel { get; }

    public SplashView()
    {
        ViewModel = App.GetService<SplashViewModel>();
        InitializeComponent();
    }
}
