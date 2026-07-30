using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Startup.ViewModels;

namespace MTM_Waitlist.Module_Startup.Views;

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
