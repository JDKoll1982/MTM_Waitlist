using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.GenericRequest.Views;

public sealed partial class GenericRequestDetailsPage : Page
{
    public GenericRequestDetailsPage()
        : this(GenericRequestPageServices.Get<GenericRequestDetailsViewModel>())
    {
    }

    public GenericRequestDetailsPage(GenericRequestDetailsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadAsync();
    }

    public GenericRequestDetailsViewModel ViewModel { get; }
}
