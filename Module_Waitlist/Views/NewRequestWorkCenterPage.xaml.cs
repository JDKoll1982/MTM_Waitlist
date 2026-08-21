using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

public sealed partial class NewRequestWorkCenterPage : Page
{
    public NewRequestWorkCenterViewModel ViewModel
    {
        get;
    }

    public NewRequestWorkCenterPage()
    {
        ViewModel = App.GetService<NewRequestWorkCenterViewModel>();
        InitializeComponent();
    }

    private void HotWorkCentersGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is WorkCenterSelectionItem item)
        {
            ViewModel.SelectWorkCenterCommand.Execute(item);
        }
    }

    private void OtherWorkCentersGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is WorkCenterSelectionItem item)
        {
            ViewModel.SelectWorkCenterCommand.Execute(item);
        }
    }
}
