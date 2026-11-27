namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// The route key of every step in the New Request wizard: the full name of the step's view model, which is how
/// this application routes every page.
/// </summary>
/// <remarks>
/// <para>
/// The step table (<c>NewRequestFlowRules</c>) names the step a flow goes to next, and each step names the one it
/// leads to. Keeping those names here means the flow rules hold no reference to a view model at all, so they stay
/// pure flow logic, and the steps do not reach each other through them.
/// </para>
/// <para>
/// These are strings rather than <c>typeof(...).FullName</c> on purpose: the routing is the same string either
/// way, and <c>NewRequestStepRoutesTests</c> fails the build-adjacent test run if one stops matching its view
/// model, so the compile-time check the type reference used to give is still made.
/// </para>
/// </remarks>
public static class NewRequestStepRoutes
{
    /// <summary>The work-centre step, where the wizard starts.</summary>
    public const string WorkCenter = "MTM_Waitlist.Module_Waitlist.ViewModels.NewRequestWorkCenterViewModel";

    /// <summary>The job-type step, which chooses the request category.</summary>
    public const string JobType = "MTM_Waitlist.Module_Waitlist.ViewModels.NewRequestJobTypeViewModel";

    /// <summary>The item step, which chooses the request item.</summary>
    public const string Item = "MTM_Waitlist.Module_Waitlist.ViewModels.NewRequestItemViewModel";

    /// <summary>The details step, which asks the item's free-text answer.</summary>
    public const string Details = "MTM_Waitlist.Module_Waitlist.ViewModels.NewRequestDetailsViewModel";

    /// <summary>The summary step, which is the last one before the request is raised.</summary>
    public const string Summary = "MTM_Waitlist.Module_Waitlist.ViewModels.NewRequestSummaryViewModel";

    /// <summary>The result step, which reports what the submission did.</summary>
    public const string Result = "MTM_Waitlist.Module_Waitlist.ViewModels.NewRequestResultViewModel";

    /// <summary>The die step, which asks which die the job carries.</summary>
    public const string Die = "MTM_Waitlist.Module_Waitlist.ViewModels.NewRequestDieViewModel";

    /// <summary>The dunnage step, which asks which dunnage part the job carries.</summary>
    public const string Dunnage = "MTM_Waitlist.Module_Waitlist.ViewModels.NewRequestDunnageViewModel";

    /// <summary>The component step, which asks which component the job carries.</summary>
    public const string Component = "MTM_Waitlist.Module_Waitlist.ViewModels.NewRequestComponentViewModel";

    /// <summary>The hopper step, which asks the size of a hopper request.</summary>
    public const string Hopper = "MTM_Waitlist.Module_Waitlist.ViewModels.NewRequestHopperViewModel";

    /// <summary>The coil-turn step, which asks the weight of a coil.</summary>
    public const string CoilTurn = "MTM_Waitlist.Module_Waitlist.ViewModels.NewRequestCoilTurnViewModel";

    /// <summary>The skid-count step, which asks how many skids a returning item is about.</summary>
    public const string SkidCount = "MTM_Waitlist.Module_Waitlist.ViewModels.NewRequestSkidCountViewModel";

    /// <summary>The part-search step, which asks which part a wrong-part request is about.</summary>
    public const string PartSearch = "MTM_Waitlist.Module_Waitlist.ViewModels.NewRequestPartSearchViewModel";
}
