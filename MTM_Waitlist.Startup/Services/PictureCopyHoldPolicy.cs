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
/// <b>Machine setup and sign-in are never held.</b> A computer the store has never held cannot be used until it is
/// set up, and nobody reaches the board until they sign in, so both surfaces are the person's next action
/// (FR-006, FR-007) and nothing may stand in front of them. Holding machine setup there read as the launch hanging
/// on its last step; holding sign-in read the same way after a setup save, because the save carries the launch on
/// to sign-in and the window stopped for the notice again instead of showing the form.
/// </para>
/// <para>
/// A stated stop and an ending are excluded for the same reason — the first already carries its own remedies, and
/// the second is the process going away. What remains is the hand-over to the main screens, where the notice is
/// the only place the refresh failure is stated.
/// </para>
/// </remarks>
public static class PictureCopyHoldPolicy
{
    /// <summary>Whether the launch window stays on screen for <paramref name="outcome"/>.</summary>
    /// <param name="outcome">Where the launch ended.</param>
    /// <param name="launchWindowIsStillTheSurface">
    /// Whether the launch window is still what the one window shows, which is the only time the statement has a
    /// surface to be read on.
    /// </param>
    /// <returns>
    /// <see langword="true"/> only for the hand-over to the main screens while the launch window is still showing.
    /// </returns>
    /// <remarks>
    /// <b>Hold only while the launch window is showing.</b> The statement is drawn by the launch window itself, and
    /// the window is what carries the acknowledgement that releases the hold. Once another stage has taken that
    /// window's place — sign-in, machine setup, a stated stop — the statement is not on screen and nothing can press
    /// it, so arming the hold waits for an acknowledgement that can never arrive and the surface it was holding never
    /// appears. That is what left the sign-in form sitting there after an accepted sign-in: the launch handed over to
    /// sign-in first, the sign-in resumed it, and the hold armed on a window that was already showing the form.
    /// </remarks>
    public static bool HoldsFor(LaunchOutcome outcome, bool launchWindowIsStillTheSurface)
        => launchWindowIsStillTheSurface && outcome is LaunchOutcome.MainScreens;
}
