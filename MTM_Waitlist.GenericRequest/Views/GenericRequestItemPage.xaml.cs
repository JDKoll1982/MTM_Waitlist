using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.GenericRequest.Views;

public sealed partial class GenericRequestItemPage : Page
{
    public GenericRequestItemPage()
        : this(GenericRequestPageServices.Get<GenericRequestItemViewModel>())
    {
    }

    public GenericRequestItemPage(GenericRequestItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadAsync();
    }

    public GenericRequestItemViewModel ViewModel { get; }

    /// <summary>
    /// The finished-product card is the only card a click has to act on: choosing any other card is the grid's own
    /// selection, but this one names no item, so clicking it opens the page that asks which kind it is.
    /// </summary>
    private void ItemGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is GenericRequestItemOption { IsFinishedProductGroup: true })
        {
            ViewModel.OpenFinishedProductChooserCommand.Execute(null);
        }
    }
}
