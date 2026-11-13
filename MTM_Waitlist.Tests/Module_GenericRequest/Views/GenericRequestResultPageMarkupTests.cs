using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_GenericRequest.Views;

/// <summary>
/// The generic request's result page is the work-order result page: one glyph, one heading, one sentence, and the same
/// buttons in the same three outcomes. Two endings for the same kind of request is exactly the drift this holds shut —
/// a person who raises a work-order request and one who raises a generic request should not have to learn the flow
/// twice.
/// </summary>
/// <remarks>
/// Read as markup rather than by activating the pages, because the work-order page lives in the WinUI application
/// project which this suite deliberately does not reference; it runs headless.
/// </remarks>
[TestClass]
public sealed class GenericRequestResultPageMarkupTests
{
    private const string GenericPagePath = @"MTM_Waitlist.GenericRequest\Views\GenericRequestResultPage.xaml";

    private const string WorkOrderPagePath = @"Module_Waitlist\Views\NewRequestResultPage.xaml";

    [TestMethod]
    public void UI_GenericRequestResultPageMarkupTests_Buttons_UseTheSameTemplatesAsTheWorkOrderResultPage()
    {
        var generic = ButtonContentTemplates(ReadMarkup(GenericPagePath));
        var workOrder = ButtonContentTemplates(ReadMarkup(WorkOrderPagePath));

        CollectionAssert.AreEqual(
            workOrder,
            generic,
            "the generic result page must offer the same buttons, in the same order, as the work-order result page");
    }

    [TestMethod]
    public void UI_GenericRequestResultPageMarkupTests_Buttons_EmphasiseTheSameActionsAsTheWorkOrderResultPage()
    {
        Assert.AreEqual(
            ButtonAccentCount(ReadMarkup(WorkOrderPagePath)),
            ButtonAccentCount(ReadMarkup(GenericPagePath)),
            "the primary action of each outcome must stand out the same way on both pages");
    }

    [TestMethod]
    public void UI_GenericRequestResultPageMarkupTests_Outcomes_DrawTheSameThreePhasesAsTheWorkOrderResultPage()
    {
        var markup = ReadMarkup(GenericPagePath);

        foreach (var phase in new[] { "IsSuccess", "IsDuplicate", "IsFailure" })
        {
            Assert.IsTrue(
                Regex.IsMatch(
                    markup,
                    $@"ViewModel\.{phase}, Mode=OneWay, Converter=\{{StaticResource BoolToVisibilityConverter\}}"),
                $"the {phase} buttons must appear only in that outcome, exactly as the work-order page does it");
        }
    }

    [TestMethod]
    public void UI_GenericRequestResultPageMarkupTests_Layout_UsesTheSameShellStylesAsTheWorkOrderResultPage()
    {
        var markup = ReadMarkup(GenericPagePath);

        foreach (var style in new[] { "SetupPageGridStyle", "SetupPageTitleStyle", "SetupPageSubtitleStyle" })
        {
            Assert.IsTrue(
                markup.Contains($"{{StaticResource {style}}}", StringComparison.Ordinal),
                $"the page must use {style}, which is what makes it look like the work-order result page");
        }

        Assert.IsTrue(
            Regex.IsMatch(markup, @"IsActive=""\{x:Bind ViewModel\.IsBusy, Mode=OneWay\}"""),
            "a retry in flight needs the same progress ring the work-order page shows");
    }

    [TestMethod]
    public void UI_GenericRequestResultPageMarkupTests_AutomationIds_AreOwnedByThisPage()
    {
        var ids = Regex.Matches(ReadMarkup(GenericPagePath), @"AutomationProperties\.AutomationId=""([^""]+)""")
            .Select(match => match.Groups[1].Value)
            .ToList();

        Assert.IsTrue(ids.Count > 0, "the page's buttons must be reachable by UI automation");

        foreach (var id in ids)
        {
            StringAssert.StartsWith(
                id,
                "GenericRequestResultPage_",
                "an automation id shared with the work-order page would target the wrong screen");
        }
    }

    private static List<string> ButtonContentTemplates(string markup) =>
        Regex.Matches(markup, @"ContentTemplate=""\{StaticResource (\w+)\}""")
            .Select(match => match.Groups[1].Value)
            .ToList();

    private static int ButtonAccentCount(string markup) =>
        Regex.Matches(markup, @"Style=""\{StaticResource AccentButtonStyle\}""").Count;

    private static string ReadMarkup(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), relativePath));
}
