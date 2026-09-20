using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

public sealed partial class NewRequestDiePage : Page
{
    public NewRequestDieViewModel ViewModel
    {
        get;
    }

    public NewRequestDiePage()
    {
        ViewModel = App.GetService<NewRequestDieViewModel>();
        InitializeComponent();
    }

    private void DieGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is NewRequestDieOption option)
        {
            ViewModel.SelectDieCommand.Execute(option);
        }
    }
}
