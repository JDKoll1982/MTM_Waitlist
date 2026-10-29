using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.ViewModels;

namespace MTM_Waitlist.GenericRequest.Temp;

/// <summary>
/// Hands the screens their view models, so a page activated through its XAML type can find one.
/// </summary>
/// <remarks>
/// This is the preview's own host. It resolves only from the view models the window already built, so nothing is
/// constructed by reflection and nothing reaches a store.
/// </remarks>
internal sealed class TempPreviewServices : IServiceProvider
{
    private readonly IReadOnlyDictionary<GenericRequestStep, GenericRequestStepViewModel> _viewModels;

    public TempPreviewServices(IReadOnlyDictionary<GenericRequestStep, GenericRequestStepViewModel> viewModels) =>
        _viewModels = viewModels;

    /// <inheritdoc />
    public object? GetService(Type serviceType) =>
        _viewModels.Values.FirstOrDefault(viewModel => viewModel.GetType() == serviceType);
}
