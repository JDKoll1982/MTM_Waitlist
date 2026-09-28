using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Module_Startup.ViewModels;

namespace MTM_Waitlist.Tests.Module_Startup.ViewModels;

/// <summary>
/// FR-013: a person who has signed in with a temporary credential chooses a new password before anything else,
/// and the panel only hands one on once it is one this screen would accept.
/// </summary>
[TestClass]
public sealed class PasswordChangeViewModelTests
{
    [TestMethod]
    public void SubmitCommand_WhenNothingWasTyped_StatesItAndHandsNothingOn()
    {
        // Arrange
        var outcome = new SignInOutcome();
        var viewModel = new PasswordChangeViewModel(outcome);
        var chosen = false;
        viewModel.PasswordChosen += (_, _) => chosen = true;

        // Act
        viewModel.SubmitCommand.Execute(new NewPasswordDraft(string.Empty, string.Empty));

        // Assert
        Assert.IsFalse(chosen);
        Assert.AreEqual("Startup_PasswordChange.PasswordRequired".GetLocalized(), viewModel.Message);
    }

    [TestMethod]
    public void SubmitCommand_WhenThePasswordIsTooShort_StatesItAndHandsNothingOn()
    {
        // Arrange
        var outcome = new SignInOutcome();
        var viewModel = new PasswordChangeViewModel(outcome);

        // Act
        viewModel.SubmitCommand.Execute(new NewPasswordDraft(new string('a', PasswordChangeViewModel.MinimumPasswordLength - 1), new string('a', PasswordChangeViewModel.MinimumPasswordLength - 1)));

        // Assert
        Assert.AreEqual("Startup_PasswordChange.PasswordTooShort".GetLocalized(), viewModel.Message);
        Assert.IsNull(outcome.NewPassword, "a password this screen would not accept must not reach the launch");
    }

    [TestMethod]
    public void SubmitCommand_WhenTheTwoValuesDiffer_StatesItAndHandsNothingOn()
    {
        // Arrange
        var outcome = new SignInOutcome();
        var viewModel = new PasswordChangeViewModel(outcome);

        // Act
        viewModel.SubmitCommand.Execute(new NewPasswordDraft("correct-horse", "correct-horsf"));

        // Assert
        Assert.AreEqual("Startup_PasswordChange.ConfirmationMismatch".GetLocalized(), viewModel.Message);
        Assert.IsNull(outcome.NewPassword);
    }

    [TestMethod]
    public void SubmitCommand_WhenThePasswordIsAccepted_HoldsItAndRaisesTheChoice()
    {
        // Arrange
        var outcome = new SignInOutcome();
        var viewModel = new PasswordChangeViewModel(outcome);
        var chosen = 0;
        viewModel.PasswordChosen += (_, _) => chosen++;

        // Act
        viewModel.SubmitCommand.Execute(new NewPasswordDraft("correct-horse", "correct-horse"));

        // Assert: the password reaches the launch's own step, which is what writes it (FR-013).
        Assert.AreEqual(1, chosen);
        Assert.AreEqual("correct-horse", outcome.NewPassword);
        Assert.IsFalse(viewModel.HasMessage);
    }
}
