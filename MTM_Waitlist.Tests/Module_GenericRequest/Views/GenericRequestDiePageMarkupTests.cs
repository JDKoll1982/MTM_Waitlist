using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_GenericRequest.Views;

/// <summary>
/// The die page draws what the step answers: a count above the list, a location line on every row, and a checkbox
/// that marks a die rather than leaving the step (020-generic-wc-die-search FR-007, FR-008, FR-010).
/// </summary>
/// <remarks>
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
    public void UI_GenericRequestDiePageMarkupTests_EveryRow_DrawsItsLocationText()
    {
        var markup = ReadMarkup();

        StringAssert.Contains(
            markup,
            @"Text=""{x:Bind LocationText}""",
            "a row draws the location wording, which says so when Visual records no location (FR-006)");
    }

    [TestMethod]
    public void UI_GenericRequestDiePageMarkupTests_EveryRow_IsAMarkableChoice()
    {
        var markup = ReadMarkup();

        StringAssert.Contains(
            markup,
            @"IsChecked=""{x:Bind IsSelected, Mode=TwoWay}""",
            "a die is marked rather than selected, because a request may need several (FR-010)");
        StringAssert.Contains(
            markup,
            @"SelectionMode=""None""",
            "the list has no single selection of its own, so the rows are the only record of a choice");
    }

    [TestMethod]
    public void UI_GenericRequestDiePageMarkupTests_TheList_ScrollsRatherThanClipping()
    {
        var markup = ReadMarkup();

        StringAssert.Contains(
            markup,
            "<ListView",
            "every match is reachable through one scrolling list (FR-008)");
        Assert.IsFalse(
            Regex.IsMatch(markup, @"MaxHeight=""\d+"""),
            "a capped list would hide the dies beyond the cap");
    }

    private static string ReadMarkup() =>
        File.ReadAllText(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), GenericPagePath));
}
