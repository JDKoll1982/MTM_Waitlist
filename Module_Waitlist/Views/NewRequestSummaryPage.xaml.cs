using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

public sealed partial class NewRequestSummaryPage : Page
{
    public NewRequestSummaryViewModel ViewModel
    {
        get;
    }

    public NewRequestSummaryPage()
    {
        ViewModel = App.GetService<NewRequestSummaryViewModel>();
        InitializeComponent();
    }
}
