using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_GenericRequest.Views;

/// <summary>
/// The details step asks how many and nothing else: the description box is gone, so no request can be held up by a
/// question nothing needs answered.
/// </summary>
/// <remarks>
/// Read as markup rather than by activating the page, because the suite runs headless. The absence is the contract
/// here: a later edit that put the box back would ask a handler's question again without anyone noticing.
/// </remarks>
[TestClass]
public sealed class GenericRequestDetailsPageMarkupTests
{
    private const string PagePath = @"MTM_Waitlist.GenericRequest\Views\GenericRequestDetailsPage.xaml";

    [TestMethod]
    public void UI_GenericRequestDetailsPageMarkupTests_TheStep_AsksHowManyAndDrawsNoDescriptionBox()
    {
        var markup = ReadMarkup();

        StringAssert.Contains(markup, "NumberBox", "the step still asks how many");
        StringAssert.Contains(markup, "{x:Bind ViewModel.Quantity, Mode=TwoWay}", "the quantity is the step's answer");
        Assert.IsFalse(
            markup.Contains("TextBox", StringComparison.Ordinal),
            "the description box is removed, not left unbound on the page");
        Assert.IsFalse(
            markup.Contains("GenericRequest_Details_Description", StringComparison.Ordinal),
            "the step no longer labels a description field");
    }

    private static string ReadMarkup() =>
        File.ReadAllText(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), PagePath));
}
