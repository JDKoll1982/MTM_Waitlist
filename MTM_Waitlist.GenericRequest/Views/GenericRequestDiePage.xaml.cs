using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.GenericRequest.Views;

public sealed partial class GenericRequestDiePage : Page
{
    public GenericRequestDiePage()
        : this(GenericRequestPageServices.Get<GenericRequestDieViewModel>())
    {
    }

    public GenericRequestDiePage(GenericRequestDieViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadAsync();
    }

    public GenericRequestDieViewModel ViewModel { get; }
}
