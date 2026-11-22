using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.GenericRequest.Views;

public sealed partial class GenericRequestProductPickupPage : Page
{
    public GenericRequestProductPickupPage()
        : this(GenericRequestPageServices.Get<GenericRequestProductPickupViewModel>())
    {
    }

    public GenericRequestProductPickupPage(GenericRequestProductPickupViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadAsync();
    }

    public GenericRequestProductPickupViewModel ViewModel { get; }
}
