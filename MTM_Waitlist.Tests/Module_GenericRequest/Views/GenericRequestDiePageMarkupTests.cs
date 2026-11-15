using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_GenericRequest.Views;

/// <summary>
/// The die page draws what the step answers: a count above the list, a location line on every card, and a card that
/// marks a die rather than leaving the step (020-generic-wc-die-search FR-007, FR-008, FR-010).
/// </summary>/// <remarks>
/// Read as markup rather than by activating the page, because the suite runs headless. The bindings are the contract:
/// a view model property that nothing binds would leave the requester looking at the old, single-selection list.
/// </remarks>
[TestClass]
public sealed class GenericRequestDiePageMarkupTests
{
    private const string GenericPagePath = @"MTM_Waitlist.GenericRequest\Views\GenericRequestDiePage.xaml";

    [TestMethod]
    public void UI_GenericRequestDiePageMarkupTests_Count_IsDrawnAboveTheList()
    {
        var markup = ReadMarkup();

        StringAssert.Contains(
            markup,
            @"Text=""{x:Bind ViewModel.CountText, Mode=OneWay}""",
            "the number of matches must be visible, so a partial list cannot pass for a complete one (FR-007)");

        var countIndex = markup.IndexOf("ViewModel.CountText", StringComparison.Ordinal);
        var listIndex = markup.IndexOf("ItemsSource=\"{x:Bind ViewModel.Options}\"", StringComparison.Ordinal);

        Assert.IsTrue(countIndex >= 0 && listIndex > countIndex, "the count is drawn before the list it counts");
    }

    [TestMethod]
    public void UI_GenericRequestDiePageMarkupTests_EveryCard_DrawsItsLocationText()
    {
        var markup = ReadMarkup();

        StringAssert.Contains(
            markup,
            @"Text=""{x:Bind LocationText}""",
            "a card draws the location wording, which says so when Visual records no location (FR-006)");
    }

    [TestMethod]
    public void UI_GenericRequestDiePageMarkupTests_EveryCard_IsAMarkableChoice()
    {
        var markup = ReadMarkup();

        StringAssert.Contains(
            markup,
            @"ItemClick=""DieGridView_ItemClick""",
            "clicking a card marks or unmarks that die, because a request may need several (FR-010)");
        StringAssert.Contains(
            markup,
            @"SelectionMode=""None""",
            "the grid has no single selection of its own, so the cards are the only record of a choice");
    }

    /// <summary>
    /// The cards are bordered surfaces that mark the chosen die, and each one carries its whole choice as one
    /// accessible name.
    /// </summary>
    /// <remarks>
    /// A check box makes a person aim at a small box inside a row, and a screen reader reads the number, the
    /// description and the location as three unrelated pieces of text. A card is the target, the state and the whole
    /// description in one surface, which is what the work center and category steps already look like.
    /// </remarks>
    [TestMethod]
    public void UI_GenericRequestDiePageMarkupTests_EachDie_IsACardThatCarriesItsWholeChoice()
    {
        var markup = ReadMarkup();

        Assert.IsFalse(
            markup.Contains("<CheckBox", StringComparison.Ordinal),
            "check boxes are the old single-selection look; the cards replace them");
        StringAssert.Contains(
            markup,
            @"AutomationProperties.Name=""{x:Bind CardName}""",
            "the die, its description and its location are announced as one choice");
        StringAssert.Contains(
            markup,
            "CardGridSizing.MinimumCardWidth",
            "the cards are laid out on the shared responsive grid the other choice steps use");
        StringAssert.Contains(
            markup,
            "IsSelected, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}",
            "the chosen card draws its own accent outline and tick from the card's flag");
    }

    [TestMethod]
    public void UI_GenericRequestDiePageMarkupTests_TheList_ScrollsRatherThanClipping()
    {
        var markup = ReadMarkup();

        StringAssert.Contains(
            markup,
            "<GridView",
            "every match is reachable through one scrolling grid (FR-008)");
        Assert.IsFalse(
            Regex.IsMatch(markup, @"MaxHeight=""\d+"""),
            "a capped list would hide the dies beyond the cap");
    }

    private static string ReadMarkup() =>
        File.ReadAllText(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), GenericPagePath));
}
