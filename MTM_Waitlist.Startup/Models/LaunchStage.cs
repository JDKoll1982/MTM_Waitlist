using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Models;

/// <summary>
/// One of the phases the single window passes through (`contracts/window-stage-contract.md` §2; FR-016, FR-017).
/// </summary>
/// <remarks>
/// <b>One window, five stages.</b> The splash is shown while the launch runs and before any outcome; the other
/// four stages are chosen from the outcome the launch ended at. The shell is a stage of the same window, so there
/// is never a second window and never a moment with none (FR-016, FR-019).
/// </remarks>
public enum LaunchStage
{
    /// <summary>The launch is running; the navigation panel is hidden and the feed is shown (FR-018).</summary>
    Splash,

    /// <summary>The launch needs a person, so the sign-in stage is shown (FR-001).</summary>
    SignIn,

    /// <summary>This computer needs setting up before anything else (FR-006).</summary>
    MachineSetup,

    /// <summary>The launch stopped, with a cause and the remedies that could remove it.</summary>
    Blocked,

    /// <summary>The shell, which is the only stage where the navigation panel is shown (FR-018, FR-019).</summary>
    Shell,
}

/// <summary>
/// Maps a launch outcome to the one stage it means (`contracts/window-stage-contract.md` §2; FR-016, FR-022).
/// </summary>
/// <remarks>
/// <para>
/// <b>The mapping is total and one-to-one.</b> Every outcome except <see cref="LaunchOutcome.Ended"/> names exactly
/// one stage, and <see cref="LaunchOutcome.Ended"/> names none because the process is going rather than a stage
/// being shown. There is no outcome that maps to <see cref="LaunchStage.Shell"/> except
/// <see cref="LaunchOutcome.MainScreens"/>, which is what keeps the shell unreachable from any other ending
/// (FR-022, FR-043).
/// </para>
/// </remarks>
public static class LaunchStageSelection
{
    /// <summary>The stage an outcome means, or <c>null</c> when the outcome ends the process.</summary>
    /// <param name="outcome">Where the launch ended.</param>
    public static LaunchStage? For(LaunchOutcome outcome) => outcome switch
    {
        LaunchOutcome.MainScreens => LaunchStage.Shell,
        LaunchOutcome.SignIn => LaunchStage.SignIn,
        LaunchOutcome.MachineSetup => LaunchStage.MachineSetup,
        LaunchOutcome.Blocked => LaunchStage.Blocked,
        LaunchOutcome.Ended => null,
        _ => null,
    };

    /// <summary>
    /// Whether the navigation panel is shown for a stage, which is only ever the shell (FR-018, FR-023).
    /// </summary>
    /// <param name="stage">The stage the window is showing.</param>
    public static bool ShowsNavigation(LaunchStage stage) => stage is LaunchStage.Shell;

    /// <summary>
    /// Whether the window maximises for a stage, which it does on the move to the shell (FR-019).
    /// </summary>
    /// <param name="stage">The stage the window is showing.</param>
    public static bool Maximises(LaunchStage stage) => stage is LaunchStage.Shell;
}
