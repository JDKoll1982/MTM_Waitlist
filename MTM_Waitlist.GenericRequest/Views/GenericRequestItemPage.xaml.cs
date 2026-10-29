using Microsoft.UI.Xaml.Controls;

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
}
