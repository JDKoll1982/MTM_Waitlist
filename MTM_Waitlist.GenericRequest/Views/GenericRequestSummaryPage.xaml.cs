using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.GenericRequest.Views;

public sealed partial class GenericRequestSummaryPage : Page
{
    public GenericRequestSummaryPage()
        : this(GenericRequestPageServices.Get<GenericRequestSummaryViewModel>())
    {
    }

    public GenericRequestSummaryPage(GenericRequestSummaryViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadAsync();
    }

    public GenericRequestSummaryViewModel ViewModel { get; }
}
