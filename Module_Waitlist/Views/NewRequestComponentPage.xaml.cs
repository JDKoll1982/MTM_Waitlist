using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

public sealed partial class NewRequestComponentPage : Page
{
    public NewRequestComponentViewModel ViewModel
    {
        get;
    }

    public NewRequestComponentPage()
    {
        ViewModel = App.GetService<NewRequestComponentViewModel>();
        InitializeComponent();
    }

    private void ComponentGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is NewRequestComponentOption option)
        {
            ViewModel.SelectComponentCommand.Execute(option);
        }
    }
}
