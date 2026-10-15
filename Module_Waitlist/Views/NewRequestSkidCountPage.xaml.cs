using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

public sealed partial class NewRequestSkidCountPage : Page
{
    public NewRequestSkidCountViewModel ViewModel
    {
        get;
    }

    public NewRequestSkidCountPage()
    {
        ViewModel = App.GetService<NewRequestSkidCountViewModel>();
        InitializeComponent();
    }
}
