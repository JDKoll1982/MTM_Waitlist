using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.ViewModels;

namespace MTM_Waitlist.Views;

public sealed partial class MainShellPage : Page
{
    public MainShellViewModel ViewModel
    {
        get;
    }

    public MainShellPage()
    {
        ViewModel = App.GetService<MainShellViewModel>();
        InitializeComponent();
    }
}
