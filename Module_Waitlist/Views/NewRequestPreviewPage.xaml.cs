using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

public sealed partial class NewRequestPreviewPage : Page
{
    public NewRequestPreviewViewModel ViewModel
    {
        get;
    }

    public NewRequestPreviewPage()
    {
        ViewModel = App.GetService<NewRequestPreviewViewModel>();
        InitializeComponent();
    }
}
