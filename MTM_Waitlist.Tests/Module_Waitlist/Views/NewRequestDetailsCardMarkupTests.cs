using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.Views;

/// <summary>
/// US5 (FR-021, FR-023): the remaining-answer list is a list of cards, each carrying its answer's picture, and
/// choosing a card still chooses the answer and still moves the flow on.
/// </summary>
/// <remarks>
/// The markup and the code-behind are read as source, the way the shell's own tests read them, because the answer
/// the card carries is the answer the step advances with — that is the part a picture-only conversion could
/// silently break, and it is a fact about the wiring rather than about a rendered control.
/// </remarks>
[TestClass]
public sealed class NewRequestDetailsCardMarkupTests
{
    [TestMethod]
    public void TheAnswerList_IsACardListCarryingEachAnswersPicture()
    {
        var cards = AnswerCards();

        Assert.IsNotNull(cards, "The answer list is a GridView of cards (FR-021).");
        Assert.AreEqual(
            "{StaticResource PartPictureCardTemplate}",
            (string?)cards.Attribute("ItemTemplate"),
            "It draws the one shared card every converted list uses.");
        Assert.AreEqual("None", (string?)cards.Attribute("SelectionMode"), "The card is chosen by clicking it, not by a selection.");
        Assert.AreEqual("True", (string?)cards.Attribute("IsItemClickEnabled"));
        Assert.IsNotNull(cards.Attribute("ItemClick"), "Choosing a card is wired to a handler.");
        StringAssert.Contains((string?)cards.Attribute("ItemsSource") ?? string.Empty, "OptionCards");
    }

    [TestMethod]
    public void TheAnswerList_IsNoLongerADropDown()
    {
        var markup = PageMarkup();

        Assert.IsFalse(markup.Contains("OptionCombo", StringComparison.Ordinal), "The drop-down is replaced, not left beside the cards.");
        Assert.IsFalse(markup.Contains("ComboBox", StringComparison.Ordinal), "No drop-down remains on the step.");
    }

    [TestMethod]
    public void ChoosingACard_StillChoosesTheAnswerAndAdvances()
    {
        var codeBehind = CodeBehind();

        StringAssert.Contains(codeBehind, "is not AnswerOptionCard card", "The handler reads the answer off the card.");
        StringAssert.Contains(codeBehind, "ViewModel.SelectAnswer(card.Answer)", "The answer the card carries is the answer chosen.");
        StringAssert.Contains(codeBehind, "ViewModel.ContinueCommand.Execute(null)", "Picking an answer is still the way out of the step.");
        StringAssert.Contains(
            codeBehind,
            "ViewModel.RestoredAnswer",
            "Coming Back to the step still stands rather than leaving again, so an answer can be corrected.");
    }

    private static XElement? AnswerCards()
    {
        var document = XDocument.Parse(PageMarkup());
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

        return document
            .Descendants()
            .FirstOrDefault(element => (string?)element.Attribute(x + "Name") == "OptionCardList");
    }

    private static string PageMarkup() =>
        File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Waitlist",
            "Views",
            "NewRequestDetailsPage.xaml"));

    private static string CodeBehind() =>
        File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Waitlist",
            "Views",
            "NewRequestDetailsPage.xaml.cs"));
}
