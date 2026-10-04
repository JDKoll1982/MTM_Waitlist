namespace MTM_Waitlist.Module_Startup.Models;

/// <summary>
/// The group a launch step belongs to (`contracts/launch-step-contract.md` §1, FR-004).
/// </summary>
/// <remarks>
/// <para>
/// The category groups the activity feed and lets a remedy be matched to a cause, so the values are the areas a
/// launch touches rather than one entry per step. The set is pinned by the contract: a step names one of these
/// and never invents its own, so a reader can filter the feed by area without knowing the step list.
/// </para>
/// </remarks>
public enum LaunchStepCategory
{
    /// <summary>Reading the application's own configuration and reaching the store that holds it.</summary>
    Configuration,

    /// <summary>This computer: its identity, its configuration and whether it is one the store knows.</summary>
    Machine,

    /// <summary>The person: who is signed in, whether their session still stands, and the sign-in itself.</summary>
    Session,

    /// <summary>The local copy of the shared pictures.</summary>
    Pictures,

    /// <summary>The read-only external system, and whether it can be reached.</summary>
    ExternalSystem,

    /// <summary>Handing the launch over to the first screen.</summary>
    Shell,
}
