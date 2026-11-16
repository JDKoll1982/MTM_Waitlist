using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_GenericRequest.Views;

/// <summary>
/// The item step's cards carry the item's name and nothing else, and its one finished-product card is the way to the
/// page that asks which kind of finished product the request is for.
/// </summary>
/// <remarks>
/// Read as markup rather than by activating the page, because the suite runs headless. The bindings are the contract:
/// a card that drew the item's catalogue code beside its name would put a code in front of the person again, and a
/// click on the finished-product card that the page did not handle would leave that card with no way forward.
/// </remarks>
[TestClass]
public sealed class GenericRequestItemPageMarkupTests
{
    private const string ItemPagePath = @"MTM_Waitlist.GenericRequest\Views\GenericRequestItemPage.xaml";

    private const string ItemCodeBehindPath = @"MTM_Waitlist.GenericRequest\Views\GenericRequestItemPage.xaml.cs";

    private const string ProductPickupPagePath = @"MTM_Waitlist.GenericRequest\Views\GenericRequestProductPickupPage.xaml";

    [TestMethod]
    public void UI_GenericRequestItemPageMarkupTests_TheCards_NameTheItemAndDrawNoSecondLine()
    {
        var document = XDocument.Load(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), ItemPagePath));
        var drawn = document
            .Descendants()
            .Where(element => element.Name.LocalName == "TextBlock")
            .Select(element => (string?)element.Attribute("Text") ?? string.Empty)
            .ToList();

        CollectionAssert.Contains(drawn, "{x:Bind Name}", "the card still names the item");
        Assert.IsFalse(
            drawn.Any(text => text.Contains("{x:Bind Code}", StringComparison.Ordinal)),
            "the smaller line under the name is gone: a person choosing an item is not reading catalogue codes");
    }

    [TestMethod]
    public void UI_GenericRequestItemPageMarkupTests_AClickOnTheFinishedProductCard_ReachesTheChosenPage()
    {
        var markup = File.ReadAllText(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), ItemPagePath));

        StringAssert.Contains(markup, @"IsItemClickEnabled=""True""", "a click is how the one card opens its page");
        StringAssert.Contains(
            markup,
            @"ItemClick=""ItemGridView_ItemClick""",
            "the click is handled, or the finished-product card has no way forward");

        var codeBehind = File.ReadAllText(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), ItemCodeBehindPath));

        StringAssert.Contains(codeBehind, "IsFinishedProductGroup", "only the finished-product card opens a page");
        StringAssert.Contains(codeBehind, "OpenFinishedProductChooserCommand", "the click reaches the view model's command");
    }

    [TestMethod]
    public void UI_GenericRequestItemPageMarkupTests_TheFinishedProductPage_AsksTheKindAndBindsItsAnswer()
    {
        var path = Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), ProductPickupPagePath);

        Assert.IsTrue(File.Exists(path), "the page the one card opens must exist.");

        var markup = File.ReadAllText(path);

        StringAssert.Contains(markup, "GenericRequestStepFrame", "the page is drawn by the same frame every step uses");
        StringAssert.Contains(markup, "{x:Bind ViewModel.Options}", "it lists the kinds to choose from");
        StringAssert.Contains(
            markup,
            @"{x:Bind ViewModel.Selected, Mode=TwoWay}",
            "the kind chosen is the page's answer");
        StringAssert.Contains(markup, "{x:Bind ViewModel.ContinueCommand}", "answering the page is how the flow moves on");
        StringAssert.Contains(markup, "{x:Bind ViewModel.BackCommand}", "and Back returns to the item step");
    }
}
