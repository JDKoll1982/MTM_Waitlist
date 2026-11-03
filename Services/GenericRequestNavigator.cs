using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.ViewModels;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Services;

/// <summary>
/// Carries one generic request between its screens and opens the flow for a work center.
/// </summary>
/// <remarks>
/// <para>
/// The flow's own contract, <see cref="IGenericRequestNavigator"/>, deliberately knows nothing about frames or pages,
/// so this is where the step becomes a route: each step names its view model, and the shell's navigation service
/// resolves that name to a page through <c>PageService</c>. The draft lives here because it is the one thing every
/// step shares and no step may clear another's answer.
/// </para>
/// <para>
/// <b>One flow at a time.</b> The instance is a singleton so every step view model resolves the same draft; a second
/// request starts by replacing the draft, which is what
/// <see cref="Module_Waitlist.Services.IGenericRequestLauncher.Launch"/> does before it moves to the first step.
/// </para>
/// </remarks>
public sealed class GenericRequestNavigator : IGenericRequestNavigator, IGenericRequestLauncher
{
    private readonly INavigationService _navigationService;

    public GenericRequestNavigator(INavigationService navigationService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }

    /// <inheritdoc />
    public GenericRequestDraft Draft { get; private set; } = new();

    /// <inheritdoc />
    public GenericRequestStep CurrentStep { get; private set; } = GenericRequestStep.WorkCenter;

    /// <inheritdoc />
    public void Launch(string workCenter, string building)
    {
        // A fresh draft, not a cleared one: an accepted warning or an answer from the previous request must not be
        // able to reach the next one (FR-011).
        Draft = new GenericRequestDraft
        {
            WorkCenter = workCenter?.Trim() ?? string.Empty,
            Building = building?.Trim() ?? string.Empty,
        };

        // The work center is already chosen, so the flow opens on the first question still to answer.
        GoTo(GenericRequestStep.Category);
    }

    /// <inheritdoc />
    public void GoTo(GenericRequestStep step)
    {
        var pageKey = PageKeyFor(step);
        if (pageKey is null)
        {
            return;
        }

        CurrentStep = step;
        _navigationService.NavigateTo(pageKey, Draft);
    }

    /// <summary>
    /// The view model whose page draws <paramref name="step"/>, or null when the step has no screen.
    /// </summary>
    /// <remarks>
    /// The name is the page key <c>PageService</c> resolves, and it is deliberately the view model's full name so an
    /// unregistered step fails loudly at navigation rather than silently showing the previous screen.
    /// </remarks>
    internal static string? PageKeyFor(GenericRequestStep step) => step switch
    {
        GenericRequestStep.WorkCenter => typeof(GenericRequestWorkCenterViewModel).FullName,
        GenericRequestStep.Category => typeof(GenericRequestCategoryViewModel).FullName,
        GenericRequestStep.Item => typeof(GenericRequestItemViewModel).FullName,
        GenericRequestStep.Part => typeof(GenericRequestPartViewModel).FullName,
        GenericRequestStep.Dunnage => typeof(GenericRequestDunnageViewModel).FullName,
        GenericRequestStep.Scrap => typeof(GenericRequestScrapTypeViewModel).FullName,
        GenericRequestStep.Die => typeof(GenericRequestDieViewModel).FullName,
        GenericRequestStep.Details => typeof(GenericRequestDetailsViewModel).FullName,
        GenericRequestStep.Summary => typeof(GenericRequestSummaryViewModel).FullName,
        GenericRequestStep.Result => typeof(GenericRequestResultViewModel).FullName,
        _ => null,
    };
}
