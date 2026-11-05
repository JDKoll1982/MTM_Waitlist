using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.Module_GenericRequest.Views;

/// <summary>
/// The generic request's dunnage step, drawn as a page of the wizard over the plant's real catalogue (FR-001).
/// </summary>
/// <remarks>
/// The page lives in the application project rather than in the generic request library, which is where this
/// repository's views and the composition root already are. Its route is contributed by the composition root's
/// dunnage catalogue registration, keyed by the view model's full name.
/// </remarks>
public sealed partial class GenericRequestDunnagePage : Page
{
    public GenericRequestDunnagePage()
        : this(GenericRequestPageServices.Get<GenericRequestDunnageViewModel>())
    {
    }

    public GenericRequestDunnagePage(GenericRequestDunnageViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadAsync();
    }

    public GenericRequestDunnageViewModel ViewModel { get; }
}
