using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.GenericRequest.Views;

public sealed partial class GenericRequestCategoryPage : Page
{
    public GenericRequestCategoryPage()
        : this(GenericRequestPageServices.Get<GenericRequestCategoryViewModel>())
    {
    }

    public GenericRequestCategoryPage(GenericRequestCategoryViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadAsync();
    }

    public GenericRequestCategoryViewModel ViewModel { get; }
}
