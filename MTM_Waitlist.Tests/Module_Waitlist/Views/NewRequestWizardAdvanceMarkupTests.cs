using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.Views;

/// <summary>
/// The advance gesture of the New Request wizard's choice steps. Picking a work centre, a category or a dunnage
/// part has always moved the flow on, so those steps never also asked for a Continue; the Item and
/// additional-details steps now work the same way — an Item tile click is the advance, and on the details step a
/// listed answer advances when it is picked and a typed answer when Enter is pressed. A Continue button
/// re-appearing on either step puts a redundant second action back in front of the person.
/// </summary>
/// <remarks>
/// Read as markup rather than by activating the pages: the wizard's pages live in the WinUI application project,
/// which this suite deliberately does not reference because it runs headless.
/// </remarks>
[TestClass]
public sealed class NewRequestWizardAdvanceMarkupTests
{
    /// <summary>The wizard steps whose way out is the choice itself rather than a button of their own.</summary>
    private static readonly string[] s_choiceSteps =
    [
        "NewRequestWorkCenterPage.xaml",
        "NewRequestJobTypePage.xaml",
        "NewRequestItemPage.xaml",
        "NewRequestDetailsPage.xaml",
        "NewRequestDunnagePage.xaml",
    ];

    [TestMethod]
    public void ChoiceSteps_DeclareNoContinueButton()
    {
        foreach (var step in s_choiceSteps)
        {
            var markup = ReadMarkup(step);

            Assert.IsFalse(
                Regex.IsMatch(markup, @"Command=""\{x:Bind ViewModel\.ContinueCommand\}"""),
                $"{step} still offers a Continue button, so the step asks for an action its choice already performs.");
        }
    }

    [TestMethod]
    public void ItemStep_AdvanceIsWiredToTheTileClick()
    {
        var markup = ReadMarkup("NewRequestItemPage.xaml");

        Assert.IsTrue(
            Regex.IsMatch(markup, @"IsItemClickEnabled=""True"""),
            "The Item grid must raise item clicks, which is what moves this step on.");
        Assert.IsTrue(
            Regex.IsMatch(markup, @"ItemClick=""\w+"""),
            "The Item grid must have its click handler wired, or the step has no way out at all.");
    }

    [TestMethod]
    public void DetailsStep_TypedAnswerAdvancesOnEnter()
    {
        var markup = ReadMarkup("NewRequestDetailsPage.xaml");

        Assert.IsTrue(
            Regex.IsMatch(markup, @"KeyDown=""\w+"""),
            "The typed answer needs a key handler: Enter is the way out of this step.");
        Assert.IsTrue(
            Regex.IsMatch(markup, @"AcceptsReturn=""False"""),
            "Enter moves the step on, so it must not also be a line break inside the answer.");
    }

    [TestMethod]
    public void DetailsStep_ListedAnswerAdvancesWhenItIsPicked()
    {
        var markup = ReadMarkup("NewRequestDetailsPage.xaml");

        // The listed answer is a list of cards (FR-021), so picking one is a click on a card rather than a
        // selection change on a drop-down — the way out of the step is the same, the gesture that takes it is not.
        Assert.IsTrue(
            Regex.IsMatch(markup, @"ItemClick=""\w+"""),
            "The listed answer needs its click handler wired: picking one is the way out of this step.");
    }

    private static string ReadMarkup(string fileName)
    {
        var path = Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), "Module_Waitlist", "Views", fileName);

        Assert.IsTrue(File.Exists(path), $"The wizard markup was not found at '{path}'.");

        return File.ReadAllText(path);
    }
}
