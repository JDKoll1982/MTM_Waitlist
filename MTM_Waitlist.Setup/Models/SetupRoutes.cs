namespace MTM_Waitlist.Module_Setup.Models;

/// <summary>
/// The route key of every page in the Setup wizard: the full name of the page's view model, which is how this
/// application routes every page.
/// </summary>
/// <remarks>
/// <para>
/// Each step names the page it leads to, and the wizard's pages can be walked in both directions, so the names
/// live together here rather than in a type reference from one step to the next. That keeps a step from depending
/// on the step it opens, which is what the dependency graph reported when every page named the next one directly.
/// </para>
/// <para>
/// These are strings rather than <c>typeof(...).FullName</c> on purpose: the routing is the same string either
/// way, and <c>SetupRoutesTests</c> fails the test run if one stops matching its view model, so the compile-time
/// check the type reference used to give is still made.
/// </para>
/// </remarks>
public static class SetupRoutes
{
    /// <summary>The work-centre step, where the wizard starts.</summary>
    public const string WorkCenter = "MTM_Waitlist.Module_Setup.ViewModels.SetupWorkCenterViewModel";

    /// <summary>The work-centre history step, which replays a setup saved earlier.</summary>
    public const string WorkCenterHistory = "MTM_Waitlist.Module_Setup.ViewModels.SetupWorkCenterHistoryViewModel";

    /// <summary>The work-order step, which chooses the job the setup belongs to.</summary>
    public const string WorkOrder = "MTM_Waitlist.Module_Setup.ViewModels.SetupWorkOrderViewModel";

    /// <summary>The dunnage-type step, which chooses the type the setup pairs with.</summary>
    public const string DunnageType = "MTM_Waitlist.Module_Setup.ViewModels.SetupDunnageTypeViewModel";

    /// <summary>The part-selection step, which chooses the parts of the chosen type.</summary>
    public const string PartSelection = "MTM_Waitlist.Module_Setup.ViewModels.SetupPartSelectionViewModel";

    /// <summary>The sequence-selection step, which chooses the sequence the parts sit at.</summary>
    public const string SequenceSelection = "MTM_Waitlist.Module_Setup.ViewModels.SetupSequenceSelectionViewModel";

    /// <summary>The review step, which is the last one before the setup is saved.</summary>
    public const string Review = "MTM_Waitlist.Module_Setup.ViewModels.SetupReviewViewModel";

    /// <summary>The completion step, which reports what the save did.</summary>
    public const string Completion = "MTM_Waitlist.Module_Setup.ViewModels.SetupCompletionViewModel";
}
