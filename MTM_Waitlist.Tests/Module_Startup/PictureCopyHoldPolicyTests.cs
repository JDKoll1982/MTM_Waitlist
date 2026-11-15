using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Startup.Models;

using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Module_Startup;

/// <summary>
/// Which launch outcomes the picture-copy statement may hold the window for.
/// </summary>
/// <remarks>
/// The statement belongs in front of a surface that says nothing about pictures and is not otherwise waiting on the
/// person. Machine setup is the exception it must never stand in front of: the computer cannot be used until it is
/// set up, so that screen is the person's next action. Holding it there read as the launch hanging on its last step
/// — the setup screen was there, behind a notice waiting to be acknowledged.
/// </remarks>
[TestClass]
public sealed class PictureCopyHoldPolicyTests
{
    [TestMethod]
    public void Service_PictureCopyHoldPolicyTests_TheMachineSetupSurfaceIsNeverHeld()
    {
        Assert.IsFalse(
            PictureCopyHoldPolicy.HoldsFor(LaunchOutcome.MachineSetup),
            "a computer that needs setting up hands over to setup on its own (FR-006)");
    }

    [TestMethod]
    public void Service_PictureCopyHoldPolicyTests_TheSurfacesThatSayNothingAboutPicturesAreHeld()
    {
        Assert.IsTrue(
            PictureCopyHoldPolicy.HoldsFor(LaunchOutcome.MainScreens),
            "the shell says nothing about the picture copies, so the window states it first");
        Assert.IsTrue(
            PictureCopyHoldPolicy.HoldsFor(LaunchOutcome.SignIn),
            "the sign-in form says nothing about the picture copies either");
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
