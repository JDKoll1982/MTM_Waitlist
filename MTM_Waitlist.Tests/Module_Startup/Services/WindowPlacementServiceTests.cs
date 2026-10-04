using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// Where the one window is placed (`contracts/window-stage-contract.md` §3; FR-020, FR-021): it opens centred on
/// the screen it starts on, re-centres after each resize the launch makes, and never fights a resize the person
/// performs.
/// </summary>
[TestClass]
public sealed class WindowPlacementServiceTests
{
    [TestMethod]
    public void ShouldRecentre_ForAResizeTheLaunchMade_AnswersTrue()
        => Assert.IsTrue(WindowPlacementService.ShouldRecentre(WindowResizeCause.Launch));

    [TestMethod]
    public void ShouldRecentre_ForAResizeThePersonPerformed_AnswersFalse()
    {
        // FR-020: re-centring applies only to resizes the launch makes, so a person who has just placed the
        // window is not moved under their own hands.
        Assert.IsFalse(WindowPlacementService.ShouldRecentre(WindowResizeCause.Person));
    }

    [TestMethod]
    public void CentreWithin_PutsTheWindowInTheMiddleOfTheDisplayItIsOn()
    {
        var display = new WindowRect(1920, 0, 1920, 1080);
        var window = new WindowRect(0, 0, 800, 600);

        var placed = WindowPlacementService.CentreWithin(window, display);

        Assert.AreEqual(1920 + ((1920 - 800) / 2), placed.X, "centred on the second screen, not the first");
        Assert.AreEqual((1080 - 600) / 2, placed.Y);
        Assert.AreEqual(800, placed.Width, "the size it was asked to place is kept");
        Assert.AreEqual(600, placed.Height);
    }

    [TestMethod]
    public void CentreWithin_KeepsAWindowBiggerThanTheDisplayInsideIt()
    {
        var display = new WindowRect(0, 0, 1280, 720);
        var window = new WindowRect(50, 50, 1600, 900);

        var placed = WindowPlacementService.CentreWithin(window, display);

        Assert.AreEqual(0, placed.X, "no part of the window is left outside the display");
        Assert.AreEqual(0, placed.Y);
        Assert.AreEqual(1280, placed.Width);
        Assert.AreEqual(720, placed.Height);
    }

    [TestMethod]
    public void CentreWithin_WithADisplayThatAnsweredNothing_LeavesTheWindowWhereItIs()
    {
        var window = new WindowRect(100, 200, 800, 600);

        var placed = WindowPlacementService.CentreWithin(window, new WindowRect(0, 0, 0, 0));

        Assert.AreEqual(window, placed, "a display with no size cannot be centred within");
    }
}
