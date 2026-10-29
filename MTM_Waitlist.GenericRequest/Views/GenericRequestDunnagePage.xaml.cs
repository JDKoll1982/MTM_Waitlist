using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.GenericRequest.Views;

public sealed partial class GenericRequestDunnagePage : Page
{
    public GenericRequestDunnagePage()
        : this(GenericRequestPageServices.Get<GenericRequestDunnageViewModel>())
    {
    }

    public GenericRequestDunnagePage(GenericRequestDunnageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadAsync();
    }

    public GenericRequestDunnageViewModel ViewModel { get; }
}
