using Microsoft.UI.Xaml;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Tests.Module_Settings.ViewModels;

/// <summary>
/// The one-time credential window (T053, FR-030, FR-032, FR-033, FR-034, SC-007).
/// </summary>
/// <remarks>
/// Printing the slip needs a window and the OS print dialog, which this suite cannot supply. What it proves is
/// everything either side of the printer: what the slip carries, that a print which cannot start costs nothing,
/// that a dismissal the window did not initiate is refused, and that closing releases the credential.
/// </remarks>
[TestClass]
public sealed class PinRevealDialogViewModelTests
{
    [TestMethod]
    public void Show_HoldsTheCredentialAndEveryFactTheWindowStates()
    {
        var viewModel = new PinRevealDialogViewModel(new PrinterStub());
        var issued = Issued();

        viewModel.Show(issued);

        Assert.IsTrue(viewModel.IsOpen);
        Assert.AreEqual("1234", viewModel.Pin);
        Assert.AreEqual("Sam Lead", viewModel.PersonName);
        Assert.AreEqual("SLEAD", viewModel.SignInName);
        Assert.AreEqual("Jane Smith", viewModel.IssuedByText);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.IssuedAtText), "The window says when it was issued.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.InstructionText), "The window says what to do with it.");
    }

    [TestMethod]
    public void Closed_DropsTheCredential_SoItIsReachableNowhereAfterwards()
    {
        var viewModel = new PinRevealDialogViewModel(new PrinterStub());
        viewModel.Show(Issued());

        viewModel.RequestClose();
        Assert.IsTrue(viewModel.IsCloseRequested, "The window's own button is the only thing that sets this.");

        viewModel.Closed();

        Assert.IsFalse(viewModel.IsOpen);
        Assert.IsNull(viewModel.Credential, "The credential is held nowhere once the window has closed (FR-030, SC-007).");
        Assert.AreEqual(string.Empty, viewModel.Pin);
        Assert.AreEqual(string.Empty, viewModel.SignInName);
        Assert.IsFalse(viewModel.IsCloseRequested, "The next window opened starts with the refusal in force again.");
    }

    [TestMethod]
    public void ACloseThatDidNotComeFromTheWindowsOwnButton_IsRefused_AndTheWindowStaysOpen()
    {
        var viewModel = new PinRevealDialogViewModel(new PrinterStub());
        viewModel.Show(Issued());

        // Escape, a tap outside and the system back button all reach this path: the flag was never set.
        viewModel.RefuseClose();

        Assert.IsTrue(viewModel.IsOpen, "A dismissal that did not come from the window's own button does not close it (FR-034).");
        Assert.IsFalse(viewModel.IsCloseRequested);
        Assert.AreEqual("1234", viewModel.Pin, "The credential is still on screen, because the window is still open.");
        Assert.AreEqual(
            "PinReveal_MustCloseDeliberately.Text".GetLocalized(),
            viewModel.MessageText,
            "The reader is told how to close it rather than left with a window that appears not to respond.");
    }

    [TestMethod]
    public async Task APrintThatCannotStart_CostsNothing_AndLeavesTheCredentialOnScreen()
    {
        var printer = new PrinterStub();
        var viewModel = new PinRevealDialogViewModel(printer);
        viewModel.Show(Issued());

        var printed = await viewModel.PrintAsync(window: null!);

        Assert.IsFalse(printed);
        Assert.AreEqual(0, printer.Calls, "A print that cannot start never reaches the printer.");
        Assert.IsTrue(viewModel.IsOpen, "Printing never closes the window, whatever it does (FR-033).");
        Assert.AreEqual("1234", viewModel.Pin, "The credential is still shown, so a lost print is not a lost credential.");
        Assert.AreEqual("PinReveal_PrintFailed.Text".GetLocalized(), viewModel.MessageText);
        Assert.IsTrue(viewModel.HasMessage);
    }

    [TestMethod]
    public async Task PrintingWhileNoWindowIsOpen_DoesNothingRatherThanFailingLoudly()
    {
        var printer = new PrinterStub();
        var viewModel = new PinRevealDialogViewModel(printer);

        Assert.IsFalse(await viewModel.PrintAsync(window: null!));
        Assert.AreEqual(0, printer.Calls);
    }

    [TestMethod]
    public void TheHandoverSlip_CarriesTheCredential_TheInstruction_AndTheSecondResetSentence()
    {
        var viewModel = new PinRevealDialogViewModel(new PrinterStub());
        viewModel.Show(Issued());

        var report = viewModel.BuildReport();
        var printed = string.Join(
            " | ",
            report.Sections.SelectMany(section =>
                section.Fields.Select(field => $"{field.Label} {field.Value}").Concat(section.Lines)));

        StringAssert.Contains(printed, "1234", "The slip carries the credential, which is the whole point of it.");
        StringAssert.Contains(printed, "SLEAD");
        StringAssert.Contains(printed, "Sam Lead");
        StringAssert.Contains(printed, viewModel.InstructionText, "The slip says to hand it over and not to file it (FR-032).");
        StringAssert.Contains(
            printed,
            viewModel.SecondResetText,
            "The slip says that a second reset replaces this one, so a filed slip is known to be superseded (FR-032).");
    }

    /// <summary>
    /// A printer that records whether it was reached. Printing to a real printer is not unit-testable, so the
    /// value of this stub is the negative case: a print that cannot start must never reach the printer at all.
    /// </summary>
    private sealed class PrinterStub : IReportPrintService
    {
        internal int Calls { get; private set; }

        public bool IsRegistered => false;

        public void Register(Window window)
        {
        }

        public void Unregister()
        {
        }

        public Task<bool> PrintAsync(Window window, PrintableReport report)
        {
            Calls++;
            return Task.FromResult(false);
        }
    }

    private static IssuedCredential Issued() => new(
        "1234",
        "Sam Lead",
        "SLEAD",
        new DateTimeOffset(2026, 9, 21, 9, 30, 0, TimeSpan.Zero),
        "Jane Smith");
}
