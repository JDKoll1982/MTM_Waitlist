using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.Controls;

/// <summary>
/// US1 check 3 (<c>contracts/verification-gates.md</c> G4 #2, contract C1). The waitlist card must offer no
/// control it cannot act on: a drawn button with no command is a promise the application does not keep.
/// </summary>
/// <remarks>
/// The check reads the markup rather than the visual tree, so it runs in the suite instead of needing a
/// signed-in shell. It is deliberately behavioural about the rule ("no Button without a Command") and
/// specific only about the two controls the defect names.
/// </remarks>
[TestClass]
public sealed class WaitlistLineCardMarkupTests
{
    private static readonly XNamespace s_presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    [TestMethod]
    public void Card_DrawsNoButtonThatCannotAct()
    {
        var card = LoadCard();
        var buttons = card.Descendants(s_presentation + "Button").ToList();

        foreach (var button in buttons)
        {
            Assert.IsNotNull(
                button.Attribute("Command"),
                $"The card draws a Button with no Command, so it cannot act: {button}");
        }
    }

    [TestMethod]
    public void Card_OffersNoCancelOrAcceptControl()
    {
        var automationNames = LoadCard()
            .Descendants()
            .Select(element => element.Attribute("AutomationProperties.Name")?.Value)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        Assert.IsFalse(
            automationNames.Contains("Cancel"),
            "The card still offers a Cancel control, which has no handler and cannot cancel anything.");

        Assert.IsFalse(
            automationNames.Contains("Accept"),
            "The card still offers an Accept control, which has no handler and cannot accept anything.");
    }

    [TestMethod]
    public void Card_ShowsTheRequestsRealLifecycleStatusInTheActionArea()
    {
        var card = LoadCard();

        Assert.IsTrue(
            card.Descendants().Any(element => element.Attribute("Text")?.Value.Contains("StatusBadgeText", StringComparison.Ordinal) == true),
            "The card's action area must show the request's real lifecycle status now that the inert buttons are gone.");

        Assert.IsTrue(
            card.Descendants().Any(element => element.Attribute("Visibility")?.Value.Contains("HasStatusBadge", StringComparison.Ordinal) == true),
            "The status must be hidden when the request carries no lifecycle status, rather than rendering an empty pill.");
    }

    private static XDocument LoadCard()
    {
        var path = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Waitlist",
            "Controls",
            "WaitlistLineCardView.xaml");

        Assert.IsTrue(File.Exists(path), $"The card markup was not found at '{path}'.");

        return XDocument.Load(path);
    }
}
