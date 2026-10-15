using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

public sealed partial class NewRequestPartSearchPage : Page
{
    public NewRequestPartSearchViewModel ViewModel
    {
        get;
    }

    public NewRequestPartSearchPage()
    {
        ViewModel = App.GetService<NewRequestPartSearchViewModel>();
        InitializeComponent();
    }

    private void MatchGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is PartMatchChoice choice)
        {
            ViewModel.SelectMatchCommand.Execute(choice);
        }
    }
}
