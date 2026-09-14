using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Module_Waitlist.Views;

/// <summary>
/// Item step of the New Request wizard. The card click <b>is</b> the advance — the step has no Continue button —
/// and an Item whose configuration is missing is still reported in place rather than failing on the way out
/// (FR-014), because the view model judges the choice before it moves the flow.
/// </summary>
public sealed partial class NewRequestItemPage : Page
{
    public NewRequestItemViewModel ViewModel
    {
        get;
    }

    // Parameterless on purpose: `Frame.Navigate` activates a page through its XAML type, so it has no way to
    // supply a constructor argument. A parameterised-only constructor throws inside the click handler, the
    // exception escapes into WinRT and the process dies with STATUS_STOWED_EXCEPTION (0xC000027B) and no log.
    // Every sibling wizard page resolves its view model the same way.
    public NewRequestItemPage()
    {
        ViewModel = App.GetService<NewRequestItemViewModel>();
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
