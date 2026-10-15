using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Startup.Views;

/// <summary>
/// Where the sign-in page puts the keyboard when the PIN stage appears.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this is read rather than driven.</b> The focus call lives in the sign-in page's code-behind, which
/// cannot be instantiated without a window and a UI thread: the page is built from XAML, drawing the stages is a
/// layout pass, and whether a control holds focus is a property of the real window. No unit test can reach any of
/// that, so the wiring is read the way this repository's other source audits read it, and each assertion names
/// the mechanism and the reason.
/// </para>
/// <para>
/// <b>The defect this pins.</b> Reported by hand on 2026-10-11: choosing one of the people offered beside the
/// form filled in the user name and showed the PIN box, and the person then had to click into the box before
/// typing. The PIN box is the only thing that stage asks for, so it is the stage's own job to put the cursor
/// there.
/// </para>
/// </remarks>
[TestClass]
public sealed class SignInPageFocusTests
{
    [TestMethod]
    public void UI_SignInPageFocusTests_ThePinBox_TakesTheKeyboardWhenTheStageAppears()
    {
        var page = Read("Module_Startup", "Views", "SignInPage.xaml.cs");

        StringAssert.Contains(
            page,
            "CredentialBox.Focus(FocusState.Programmatic)",
            "The PIN box is never given the keyboard, so the person has to click into it before typing (FR-014, FR-048).");

        // The focus belongs to the stage appearing, not to each redraw: a redraw happens whenever either view
        // model's flags change, and a box that took the keyboard on every one of them would fight a person who
        // had moved on to something else on the page.
        StringAssert.Contains(
            page,
            "credentialWasVisible",
            "The PIN box takes the keyboard on every redraw rather than when the stage appears (FR-014).");

        StringAssert.Contains(
            page,
            "CredentialPanel.Visibility == Visibility.Visible",
            "The stage's own visibility is not what the focus is decided from (FR-014).");
    }

    [TestMethod]
    public void UI_SignInPageFocusTests_TheRememberedPerson_LeavesThePinToBeTyped()
    {
        var page = Read("Module_Startup", "Views", "SignInPage.xaml.cs");

        // Choosing a remembered person fills in the user name and nothing else, which is why the PIN box is what
        // the stage then has to focus (FR-014, FR-048): the command is the same one the identifier stage uses, so
        // the focus rule written for the stage covers both routes into it.
        StringAssert.Contains(
            page,
            "ViewModel.ChoosePreviousPersonCommand.Execute(person)",
            "The remembered person is no longer the route that fills in the user name (FR-048).");

        var viewModel = Read("MTM_Waitlist.Startup", "ViewModels", "SignInViewModel.cs");

        StringAssert.Contains(
            viewModel,
            "IsCredentialStage",
            "The stage the PIN box belongs to is not decided by the view model any more (FR-014).");
    }

    private static string Read(params string[] parts)
    {
        var path = Path.Combine(new[] { RepositoryPatternScan.FindRepositoryRoot() }.Concat(parts).ToArray());

        Assert.IsTrue(File.Exists(path), $"the file this audit reads is not where it was: {path}");

        return File.ReadAllText(path);
    }
}
