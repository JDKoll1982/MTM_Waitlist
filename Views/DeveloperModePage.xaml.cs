using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.ViewModels;

namespace MTM_Waitlist.Views;

public sealed partial class DeveloperModePage : Page
{
    public DeveloperModeViewModel ViewModel
    {
        get;
    }

    public DeveloperModePage()
    {
        ViewModel = App.GetService<DeveloperModeViewModel>();
        InitializeComponent();
    }
}
