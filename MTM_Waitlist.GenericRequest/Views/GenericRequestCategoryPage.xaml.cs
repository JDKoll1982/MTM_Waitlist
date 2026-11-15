using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.GenericRequest.Views;

public sealed partial class GenericRequestCategoryPage : Page
{
    public GenericRequestCategoryPage()
        : this(GenericRequestPageServices.Get<GenericRequestCategoryViewModel>())
    {
    }

    public GenericRequestCategoryPage(GenericRequestCategoryViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadAsync();
    }

    public GenericRequestCategoryViewModel ViewModel { get; }

    /// <summary>
    /// Choose the category whose card was clicked, the way the work center step's cards choose a work center.
    /// </summary>
    /// <remarks>
    /// The click is handled here rather than through the grid's selection because the card carries the choice: the
    /// accent outline follows the card's own flag, and a second click on the same card still reads as a choice.
    /// </remarks>
    private void CategoryGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is GenericRequestCategoryOption option)
        {
            ViewModel.SelectCategoryCommand.Execute(option);
        }
    }
}
