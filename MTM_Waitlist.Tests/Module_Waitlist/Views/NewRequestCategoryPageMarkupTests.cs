using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.Views;

/// <summary>
/// The New Request Category step is drawn the way the generic flow's category step is: one card per category, an
/// accent outline on the chosen card, and a click that chooses rather than the grid's own selection.
/// </summary>
/// <remarks>
/// Read as markup rather than by activating the page, because the wizard's pages live in the WinUI application
/// project, which this suite deliberately does not reference. The two category steps are read by matching tests so
/// neither can drift away from the other.
/// </remarks>
[TestClass]
public sealed class NewRequestCategoryPageMarkupTests
{
    [TestMethod]
    public void UI_NewRequestCategoryPageMarkupTests_Cards_AreClickedRatherThanSelectedByTheGrid()
    {
        var markup = ReadMarkup();

        StringAssert.Contains(markup, @"SelectionMode=""None""", "the grid keeps no selection of its own");
        StringAssert.Contains(markup, @"IsItemClickEnabled=""True""", "a click is the choice");
        StringAssert.Contains(
            markup,
            @"ItemClick=""JobTypeGridView_ItemClick""",
            "the click is handled so the card's own flag decides the outline");
        StringAssert.Contains(
            markup,
            @"ItemContainerStyle=""{StaticResource CategoryCardItemContainerStyle}""",
            "the item container draws nothing of its own, as on the generic category step");
    }

    [TestMethod]
    public void UI_NewRequestCategoryPageMarkupTests_TheChosenCard_CarriesTheSameAccentOutlineAsTheGenericStep()
    {
        var markup = ReadMarkup();

        Assert.IsTrue(
            Regex.IsMatch(
                markup,
                @"Visibility=""\{Binding IsSelected, Mode=OneWay, Converter=\{StaticResource BoolToVisibilityConverter\}\}"""),
            "the outline follows the card's own IsSelected");
        StringAssert.Contains(
            markup,
            "{ThemeResource SystemControlHighlightAccentBrush}",
            "the outline is the accent the generic step's chosen card draws");
    }

    [TestMethod]
    public void UI_NewRequestCategoryPageMarkupTests_TheCards_SizeThemselvesLikeTheGenericCategoryGrid()
    {
        var markup = ReadMarkup();

        StringAssert.Contains(
            markup,
            @"shared:CardGridSizing.MaximumColumns=""3""",
            "the grid caps at the same three columns the generic step uses");
        StringAssert.Contains(
            markup,
            @"shared:CardGridSizing.MinimumCardWidth=""280""",
            "the cards wrap to the width they are given, through the same sizing behaviour");
        StringAssert.Contains(markup, "<ItemsWrapGrid", "the cards wrap rather than forming one column");
    }

    [TestMethod]
    public void UI_NewRequestCategoryPageMarkupTests_AClickedCard_ChoosesTheCategoryOnThePageCodeBehind()
    {
        var codeBehind = File.ReadAllText(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), CodeBehindPath));

        StringAssert.Contains(
            codeBehind,
            "SelectOptionCommand",
            "the click must reach the view model's command, which marks the card and moves the flow on");
    }

    private static string ReadMarkup() =>
        File.ReadAllText(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), PagePath));

    private const string PagePath = @"Module_Waitlist\Views\NewRequestJobTypePage.xaml";

    private const string CodeBehindPath = @"Module_Waitlist\Views\NewRequestJobTypePage.xaml.cs";
}
