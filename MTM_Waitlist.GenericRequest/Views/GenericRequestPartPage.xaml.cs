using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.GenericRequest.Views;

public sealed partial class GenericRequestPartPage : Page
{
    public GenericRequestPartPage()
        : this(GenericRequestPageServices.Get<GenericRequestPartViewModel>())
    {
    }

    public GenericRequestPartPage(GenericRequestPartViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadAsync();
    }

    public GenericRequestPartViewModel ViewModel { get; }
}
