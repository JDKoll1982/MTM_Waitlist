using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.GenericRequest.Views;

public sealed partial class GenericRequestResultPage : Page
{
    public GenericRequestResultPage()
        : this(GenericRequestPageServices.Get<GenericRequestResultViewModel>())
    {
    }

    public GenericRequestResultPage(GenericRequestResultViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadAsync();
    }

    public GenericRequestResultViewModel ViewModel { get; }
}
