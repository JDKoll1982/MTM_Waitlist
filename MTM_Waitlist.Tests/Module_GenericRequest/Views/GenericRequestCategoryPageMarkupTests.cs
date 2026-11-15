using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_GenericRequest.Views;

/// <summary>
/// The category page is drawn the way the work center page is: one card per choice, an accent outline on the chosen
/// card, and a click that chooses rather than the grid's own selection.
/// </summary>
/// <remarks>
/// Read as markup rather than by activating the page, because the suite runs headless. The bindings are the contract:
/// a card whose outline followed the grid's selection instead of its own flag would lose the choice when the list is
/// reloaded, which is the defect this holds shut.
/// </remarks>
[TestClass]
public sealed class GenericRequestCategoryPageMarkupTests
{
    private const string GenericPagePath = @"MTM_Waitlist.GenericRequest\Views\GenericRequestCategoryPage.xaml";

    private const string CodeBehindPath = @"MTM_Waitlist.GenericRequest\Views\GenericRequestCategoryPage.xaml.cs";

    [TestMethod]
    public void UI_GenericRequestCategoryPageMarkupTests_Cards_AreClickedRatherThanSelectedByTheGrid()
    {
        var markup = ReadMarkup();

        StringAssert.Contains(markup, @"SelectionMode=""None""", "the grid keeps no selection of its own");
        StringAssert.Contains(markup, @"IsItemClickEnabled=""True""", "a click is the choice");
        StringAssert.Contains(
            markup,
            @"ItemClick=""CategoryGridView_ItemClick""",
            "the click is handled so the card's own flag decides the outline");
    }

    [TestMethod]
    public void UI_GenericRequestCategoryPageMarkupTests_TheChosenCard_CarriesTheWorkCentersAccentOutline()
    {
        var markup = ReadMarkup();

        Assert.IsTrue(
            Regex.IsMatch(
                markup,
                @"Visibility=""\{x:Bind IsSelected, Mode=OneWay, Converter=\{StaticResource BoolToVisibilityConverter\}\}"""),
            "the outline follows the card's own IsSelected, as the work center cards do");
        StringAssert.Contains(
            markup,
            "{ThemeResource SystemControlHighlightAccentBrush}",
            "the outline is the same accent the work center's chosen card draws");
    }

    [TestMethod]
    public void UI_GenericRequestCategoryPageMarkupTests_TheCards_SizeThemselvesLikeTheWorkCenterGrid()
    {
        var markup = ReadMarkup();

        StringAssert.Contains(
            markup,
            "shared:CardGridSizing.MinimumCardWidth",
            "the cards wrap to the width they are given, through the same sizing behaviour the work center grid uses");
        StringAssert.Contains(markup, "<ItemsWrapGrid", "the cards wrap rather than forming one column");
    }

    [TestMethod]
    public void UI_GenericRequestCategoryPageMarkupTests_AClickedCard_ChoosesTheCategoryOnThePageCodeBehind()
    {
        var codeBehind = File.ReadAllText(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), CodeBehindPath));

        StringAssert.Contains(
            codeBehind,
            "SelectCategoryCommand",
            "the click must reach the view model's command, which marks the card and notifies Continue");
    }

    private static string ReadMarkup() =>
        File.ReadAllText(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), GenericPagePath));
}
