using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// Which launch outcomes the picture-copy statement may hold the launch window for.
/// </summary>
/// <remarks>
/// <para>
/// The statement exists because the refresh is best effort (FR-026) and the surface that replaces the launch window
/// says nothing about pictures. It therefore belongs in front of a surface that is otherwise silent about them and
/// is not waiting on the person.
/// </para>
/// <para>
/// <b>Machine setup is never held.</b> A computer the store has never held cannot be used until it is set up, so
/// that surface is the person's next action (FR-006, FR-007) and nothing may stand in front of it. Holding it there
/// read as the launch hanging on its last step: the setup screen existed, behind a notice waiting to be
/// acknowledged. A stated stop and an ending are excluded for the same reason — the first already carries its own
/// remedies, and the second is the process going away.
/// </para>
/// </remarks>
public static class PictureCopyHoldPolicy
{
    /// <summary>Whether the launch window stays on screen for <paramref name="outcome"/>.</summary>
    /// <param name="outcome">Where the launch ended.</param>
    /// <returns><see langword="true"/> for the surfaces that say nothing about pictures and expect nothing first.</returns>
    public static bool HoldsFor(LaunchOutcome outcome) =>
        outcome is LaunchOutcome.MainScreens or LaunchOutcome.SignIn;
}
