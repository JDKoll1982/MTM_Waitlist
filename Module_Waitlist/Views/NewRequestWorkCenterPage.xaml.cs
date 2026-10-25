using System.Collections.Specialized;

using Microsoft.UI.Xaml;
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

    /// <summary>
    /// Rebuilds the building flyout's cards as it opens, so the building in effect is never among them (FR-022).
    /// </summary>
    private void BuildingFlyout_Opening(object? sender, object e) => ViewModel.RefreshBuildingCards();

    /// <summary>Chooses the building whose card was clicked, which is the same choice the drop-down used to make.</summary>
    private void BuildingCard_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is BuildingCardOption card)
        {
            ViewModel.SelectBuilding(card.Building);
        }
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
