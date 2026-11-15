using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Startup.Models;

using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Module_Startup;

/// <summary>
/// Which launch outcomes the picture-copy statement may hold the window for.
/// </summary>
/// <remarks>
/// The statement belongs in front of a surface that says nothing about pictures and expects nothing from the person
/// first, and it can only be read while the launch window is the surface. Only the hand-over to the main screens
/// qualifies on both counts: machine setup and sign-in are both things the person has to deal with before the board,
/// and holding either read as the launch hanging — the setup screen on a first launch, and the sign-in form right
/// after a setup save carried the launch on.
/// </remarks>
[TestClass]
public sealed class PictureCopyHoldPolicyTests
{
    /// <summary>The launch window is what the one window shows, which is when the statement has a surface.</summary>
    private const bool WindowIsTheSurface = true;

    /// <summary>Sign-in, machine setup or a stated stop has taken the launch window's place.</summary>
    private const bool AnotherStageIsTheSurface = false;

    [TestMethod]
    public void Service_PictureCopyHoldPolicyTests_TheSurfacesThatNeedThePersonAreNeverHeld()
    {
        Assert.IsFalse(
            PictureCopyHoldPolicy.HoldsFor(LaunchOutcome.MachineSetup, WindowIsTheSurface),
            "a computer that needs setting up hands over to setup on its own (FR-006)");

        Assert.IsFalse(
            PictureCopyHoldPolicy.HoldsFor(LaunchOutcome.SignIn, WindowIsTheSurface),
            "nobody reaches the board without signing in, so the form comes up on its own (FR-001)");
    }

    [TestMethod]
    public void Service_PictureCopyHoldPolicyTests_TheHandOverToTheMainScreensIsHeld()
    {
        Assert.IsTrue(
            PictureCopyHoldPolicy.HoldsFor(LaunchOutcome.MainScreens, WindowIsTheSurface),
            "the shell says nothing about the picture copies, so the window states it first");
    }

    [TestMethod]
    public void Service_PictureCopyHoldPolicyTests_AMainScreensHandOverIsNotHeldOnceAnotherStageIsShowing()
    {
        Assert.IsFalse(
            PictureCopyHoldPolicy.HoldsFor(LaunchOutcome.MainScreens, AnotherStageIsTheSurface),
            "the statement and its acknowledgement are on the launch window, so a window showing the sign-in form "
            + "cannot hold a launch that never comes back (the accepted sign-in on 2026-11-15)");
    }

    [TestMethod]
    public void Service_PictureCopyHoldPolicyTests_AnEndingOrAStatedStopIsNeverHeld()
    {
        Assert.IsFalse(
            PictureCopyHoldPolicy.HoldsFor(LaunchOutcome.Blocked, WindowIsTheSurface),
            "a stated stop is already the person's next action, with its remedies");
        Assert.IsFalse(
            PictureCopyHoldPolicy.HoldsFor(LaunchOutcome.Ended, WindowIsTheSurface),
            "the process is going away, so there is nothing to hold");
    }
}
