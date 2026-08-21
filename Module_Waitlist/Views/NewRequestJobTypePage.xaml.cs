using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

public sealed partial class NewRequestJobTypePage : Page
{
    public NewRequestJobTypeViewModel ViewModel
    {
        get;
    }

    public NewRequestJobTypePage()
    {
        ViewModel = App.GetService<NewRequestJobTypeViewModel>();
        InitializeComponent();
    }

    private void JobTypeGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is NewRequestOptionItem item)
        {
            ViewModel.SelectJobTypeCommand.Execute(item);
        }
    }
}
