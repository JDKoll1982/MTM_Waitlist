using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.GenericRequest.Views;

public sealed partial class GenericRequestScrapTypePage : Page
{
    public GenericRequestScrapTypePage()
        : this(GenericRequestPageServices.Get<GenericRequestScrapTypeViewModel>())
    {
    }

    public GenericRequestScrapTypePage(GenericRequestScrapTypeViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadAsync();
    }

    public GenericRequestScrapTypeViewModel ViewModel { get; }
}
