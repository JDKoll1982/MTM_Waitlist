using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

public sealed partial class NewRequestSubtypePage : Page
{
    public NewRequestSubtypeViewModel ViewModel
    {
        get;
    }

    public NewRequestSubtypePage()
    {
        ViewModel = App.GetService<NewRequestSubtypeViewModel>();
        InitializeComponent();
    }

    private void SubtypeGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is NewRequestOptionItem item)
        {
            ViewModel.SelectSubtypeCommand.Execute(item);
        }
    }
}
