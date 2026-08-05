using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Setup.ViewModels;

namespace MTM_Waitlist.Module_Setup.Views;

public sealed partial class SetupReviewPage : Page
{
    public SetupReviewViewModel ViewModel { get; }

    public SetupReviewPage()
    {
        ViewModel = App.GetService<SetupReviewViewModel>();
        InitializeComponent();
    }
}