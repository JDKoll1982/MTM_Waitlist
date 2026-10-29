using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.GenericRequest.Views;

public sealed partial class GenericRequestWorkCenterPage : Page
{
    public GenericRequestWorkCenterPage()
        : this(GenericRequestPageServices.Get<GenericRequestWorkCenterViewModel>())
    {
    }

    public GenericRequestWorkCenterPage(GenericRequestWorkCenterViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadAsync();
    }

    public GenericRequestWorkCenterViewModel ViewModel { get; }
}
