using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

/// <summary>
/// Item step of the New Request wizard. A card click selects the Item; the flow advances on Continue, so an Item
/// whose configuration is missing can be reported in place rather than failing on the way out (FR-014).
/// </summary>
public sealed partial class NewRequestItemPage : Page
{
    public NewRequestItemViewModel ViewModel { get; }

    public NewRequestItemPage(NewRequestItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    private void ItemGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is NewRequestItemOption option)
        {
            ViewModel.SelectItemCommand.Execute(option);
        }
    }
}
