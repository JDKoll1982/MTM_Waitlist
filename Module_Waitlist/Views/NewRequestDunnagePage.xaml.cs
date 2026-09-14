using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

public sealed partial class NewRequestDunnagePage : Page
{
    public NewRequestDunnageViewModel ViewModel
    {
        get;
    }

    public NewRequestDunnagePage()
    {
        ViewModel = App.GetService<NewRequestDunnageViewModel>();
        InitializeComponent();
    }

    private void DunnageGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is NewRequestDunnageOption option)
        {
            ViewModel.SelectPartCommand.Execute(option);
        }
    }
}
