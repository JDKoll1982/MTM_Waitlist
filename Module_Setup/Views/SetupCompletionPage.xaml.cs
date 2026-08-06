using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Setup.ViewModels;

namespace MTM_Waitlist.Module_Setup.Views;

public sealed partial class SetupCompletionPage : Page
{
    public SetupCompletionViewModel ViewModel { get; }

    public SetupCompletionPage()
    {
        ViewModel = App.GetService<SetupCompletionViewModel>();
        InitializeComponent();
    }
}
