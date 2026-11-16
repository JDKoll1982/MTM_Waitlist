using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_GenericRequest.Views;

/// <summary>
/// The part step puts the sequences the part stands at beneath the part number box, one card each, and every card
/// says where that sequence is inventoried.
/// </summary>
/// <remarks>
/// Read as markup rather than by activating the page, because the suite runs headless. The order of the two elements
/// is the contract here: the sequences used to sit in a column beside the search box, and the requester read them as a
/// list rather than as the set of choices they are.
/// </remarks>
[TestClass]
public sealed class GenericRequestPartPageMarkupTests
{
    private const string PagePath = @"MTM_Waitlist.GenericRequest\Views\GenericRequestPartPage.xaml";

    [TestMethod]
    public void UI_GenericRequestPartPageMarkupTests_TheSequences_AreBeneathThePartNumberBox()
    {
        var markup = ReadMarkup();

        var searchBox = markup.IndexOf("<AutoSuggestBox", StringComparison.Ordinal);
        var sequences = markup.IndexOf("ItemsSource=\"{x:Bind ViewModel.Sequences}\"", StringComparison.Ordinal);

        Assert.IsTrue(searchBox >= 0, "the part number box must still be on the page");
        Assert.IsTrue(sequences > searchBox, "the sequences must follow the part number box, not sit beside it");

        var cardGrid = XDocument.Parse(markup)
            .Descendants()
            .Single(element => (string?)element.Attribute("ItemsSource") == "{x:Bind ViewModel.Sequences}");

        Assert.AreEqual(
            "StackPanel",
            cardGrid.Parent?.Name.LocalName,
            "the card grid stacks under the part number box rather than sitting in a column beside it");
        Assert.IsNull(
            cardGrid.Parent?.Attribute("Orientation"),
            "that stack is vertical, which is what puts the sequences below the box");
    }

    [TestMethod]
    public void UI_GenericRequestPartPageMarkupTests_EachSequence_IsItsOwnCard()
    {
        var markup = ReadMarkup();

        StringAssert.Contains(markup, "<ItemsWrapGrid", "the cards wrap rather than stacking in one list column");
        StringAssert.Contains(
            markup,
            "shared:CardGridSizing.MinimumCardWidth",
            "the cards size themselves like every other card grid");
        StringAssert.Contains(markup, @"ItemClick=""OnSequenceClicked""", "clicking a card is how its quantity is read");
        StringAssert.Contains(markup, @"{x:Bind Sequence}", "each card is titled by its sequence number");
        StringAssert.Contains(
            markup,
            "{x:Bind InventoriedWith}",
            "each card says which systems hold the sequence");
        Assert.IsFalse(
            markup.Contains("{x:Bind SourcesDisplay}", StringComparison.Ordinal),
            "the inventoried-with line replaces the old source line rather than sitting beside it");
    }

    private static string ReadMarkup() =>
        File.ReadAllText(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), PagePath));
}
