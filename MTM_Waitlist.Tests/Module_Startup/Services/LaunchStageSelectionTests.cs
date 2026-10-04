using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The one window's stages (`contracts/window-stage-contract.md` §§2-3; FR-016, FR-018, FR-019, FR-022):
/// every outcome maps to exactly one stage, the navigation panel is shown only for the shell, and the window
/// maximises only when it hands over.
/// </summary>
[TestClass]
public sealed class LaunchStageSelectionTests
{
    [TestMethod]
    [DataRow(LaunchOutcome.MainScreens, LaunchStage.Shell)]
    [DataRow(LaunchOutcome.SignIn, LaunchStage.SignIn)]
    [DataRow(LaunchOutcome.MachineSetup, LaunchStage.MachineSetup)]
    [DataRow(LaunchOutcome.Blocked, LaunchStage.Blocked)]
    public void For_EachOutcome_NamesExactlyOneStage(LaunchOutcome outcome, LaunchStage expected)
        => Assert.AreEqual(expected, LaunchStageSelection.For(outcome));

    [TestMethod]
    public void For_AnEnding_NamesNoStageBecauseTheProcessIsGoing()
        => Assert.IsNull(LaunchStageSelection.For(LaunchOutcome.Ended));

    [TestMethod]
    public void TheShell_IsReachableOnlyFromTheMainScreensOutcome()
    {
        // FR-022, FR-043: no other outcome may name the shell, which is what keeps a launch that did not reach
        // the main screens away from it.
        foreach (var outcome in Enum.GetValues<LaunchOutcome>())
        {
            if (outcome is LaunchOutcome.MainScreens)
            {
                continue;
            }

            Assert.AreNotEqual(
                LaunchStage.Shell,
                LaunchStageSelection.For(outcome),
                $"'{outcome}' must not reach the shell");
        }
    }

    [TestMethod]
    public void TheNavigationPanel_IsShownForTheShellAndNoOtherStage()
    {
        // FR-018, FR-023: nothing in the shell may assume a signed-in person before the hand-over, so the panel
        // is absent everywhere except the shell.
        foreach (var stage in Enum.GetValues<LaunchStage>())
        {
            Assert.AreEqual(
                stage is LaunchStage.Shell,
                LaunchStageSelection.ShowsNavigation(stage),
                $"navigation visibility is wrong for '{stage}'");
        }
    }

    [TestMethod]
    public void TheWindow_MaximisesOnlyOnTheMoveToTheShell()
    {
        // FR-019.
        foreach (var stage in Enum.GetValues<LaunchStage>())
        {
            Assert.AreEqual(
                stage is LaunchStage.Shell,
                LaunchStageSelection.Maximises(stage),
                $"maximise-on-entry is wrong for '{stage}'");
        }
    }
}
