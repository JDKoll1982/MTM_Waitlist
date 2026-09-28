namespace MTM_Waitlist.Module_Startup.Models;

/// <summary>
/// Where a launch ended: the five terminal outcomes of FR-001, and the whole of them
/// (`contracts/launch-step-contract.md` §5).
/// </summary>
/// <remarks>
/// <para>
/// <b>A launch ends at exactly one of these.</b> There is no sixth "still going" value and no value that means
/// "nothing happened", because a launch that ended without one of these would be the silent stop SC-001
/// forbids. <see cref="MainScreens"/> and <see cref="SignIn"/> and <see cref="MachineSetup"/> are places the
/// person is handed to; <see cref="Blocked"/> is a stated stop with remedies; <see cref="Ended"/> is the
/// process going away after the reason has been stated (FR-008).
/// </para>
/// <para>
/// <b>The surface shown follows the outcome.</b> A launch that ends at <see cref="MachineSetup"/> hands the
/// person to setup and no further, which is how a machine with no configuration is kept away from the shell
/// (FR-006): the pipeline decides, not the screen.
/// </para>
/// </remarks>
public enum LaunchOutcome
{
    /// <summary>The launch reached the main screens, and the shell is what the person sees (FR-001).</summary>
    MainScreens,

    /// <summary>The launch needs a person, so the sign-in surface is what is shown (FR-001).</summary>
    SignIn,

    /// <summary>
    /// This computer needs setting up before anything else can happen, so setup is what is shown — before any
    /// operator signs in (FR-001, FR-007).
    /// </summary>
    MachineSetup,

    /// <summary>
    /// The launch stopped, and the stop has a cause specific to it and the remedies that could remove it
    /// (FR-001, FR-004, FR-016, FR-017).
    /// </summary>
    Blocked,

    /// <summary>
    /// The process is ending, and the reason was stated before it went, so an abort does not read as a crash
    /// (FR-001, FR-008).
    /// </summary>
    Ended,
}
