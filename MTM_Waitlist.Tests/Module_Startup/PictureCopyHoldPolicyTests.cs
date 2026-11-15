using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Startup.Models;

using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Module_Startup;

/// <summary>
/// Which launch outcomes the picture-copy statement may hold the window for.
/// </summary>
/// <remarks>
/// The statement belongs in front of a surface that says nothing about pictures and expects nothing from the person
/// first. Only the hand-over to the main screens qualifies: machine setup and sign-in are both things the person
/// has to deal with before the board, and holding either read as the launch hanging — the setup screen on a first
/// launch, and the sign-in form right after a setup save carried the launch on.
/// </remarks>
[TestClass]
public sealed class PictureCopyHoldPolicyTests
{
    [TestMethod]
    public void Service_PictureCopyHoldPolicyTests_TheSurfacesThatNeedThePersonAreNeverHeld()
    {
        Assert.IsFalse(
            PictureCopyHoldPolicy.HoldsFor(LaunchOutcome.MachineSetup),
            "a computer that needs setting up hands over to setup on its own (FR-006)");

        Assert.IsFalse(
            PictureCopyHoldPolicy.HoldsFor(LaunchOutcome.SignIn),
            "nobody reaches the board without signing in, so the form comes up on its own (FR-001)");
    }

    [TestMethod]
    public void Service_PictureCopyHoldPolicyTests_TheHandOverToTheMainScreensIsHeld()
    {
        Assert.IsTrue(
            PictureCopyHoldPolicy.HoldsFor(LaunchOutcome.MainScreens),
            "the shell says nothing about the picture copies, so the window states it first");
    }

    [TestMethod]
    public void Service_PictureCopyHoldPolicyTests_AnEndingOrAStatedStopIsNeverHeld()
    {
        Assert.IsFalse(
            PictureCopyHoldPolicy.HoldsFor(LaunchOutcome.Blocked),
            "a stated stop is already the person's next action, with its remedies");
        Assert.IsFalse(
            PictureCopyHoldPolicy.HoldsFor(LaunchOutcome.Ended),
            "the process is going away, so there is nothing to hold");
    }
}
