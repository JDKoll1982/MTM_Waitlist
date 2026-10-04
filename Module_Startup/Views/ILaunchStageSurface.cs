namespace MTM_Waitlist.Module_Startup.Views;

/// <summary>
/// A launch stage the one window hosts, which answers the window's own close affordance
/// (`contracts/window-stage-contract.md` §1; FR-008, FR-016).
/// </summary>
/// <remarks>
/// <para>
/// <b>One window means one close box, and it belongs to every stage.</b> A launch runs in the same window the
/// shell will use, so the close box the person reaches during a stage is that window's, not a stage's own. The
/// host asks the stage that is showing what closing it means, so each stage keeps stating its own reason —
/// machine setup states the route it was dismissed by, a stopped launch states its cause, the sign-in states
/// that nobody signed in — instead of the host inventing one reason for all of them (FR-008).
/// </para>
/// <para>
/// <b>The stage ends the process; it does not close a window.</b> Every implementation states the reason
/// through the launch, which is the only thing that may end the process, and the host has already cancelled the
/// close so the window does not disappear before the reason has been stated.
/// </para>
/// </remarks>
internal interface ILaunchStageSurface
{
    /// <summary>
    /// States why the process is ending because the one window was closed while this stage was showing.
    /// </summary>
    Task RequestCloseAsync();
}
