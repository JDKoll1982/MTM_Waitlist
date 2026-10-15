using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Waitlist.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.Controls;

/// <summary>
/// US4 (FR-070 to FR-076, FR-102): the status word's conditions flyout — the four conditions, the gate that
/// opens it only for the row's own material handler, the keyboard path into it, and the announcement that says
/// whether the word is operable at all.
/// </summary>
/// <remarks>
/// The flyout lives in the application project, which this suite deliberately does not reference, so the control
/// and its host are read from disk: the markup as XML where containment matters, and the code-behind as text
/// where the behaviour that cannot be seen in markup lives. What the control *cannot* decide for itself — the
/// four strings — is checked only as far as its binding, because <c>HandlerStatusCatalog</c> owns them and its
/// own tests pin them verbatim and in order.
/// </remarks>
[TestClass]
public sealed class WaitlistStatusFlyoutTests
{
    private static readonly XNamespace s_presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace s_xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    // ── The four conditions, from the one vocabulary, in their settled order (FR-071, FR-102) ───────────

    [TestMethod]
    public void UI_WaitlistStatusFlyoutTests_Flyout_OffersTheFourConditionsFromTheSharedVocabularyRatherThanARelistedSet()
    {
        var markup = FlyoutMarkup();
        var codeBehind = FlyoutCodeBehind();

        StringAssert.Contains(
            markup,
            "ItemsSource=\"{Binding Conditions, ElementName=Root}\"",
            "The flyout must offer the vocabulary it is handed rather than a set of its own (FR-071, FR-102).");
        StringAssert.Contains(
            codeBehind,
            "HandlerStatusCatalog.Conditions",
            "The flyout's own default must be the shared vocabulary, so the picker that writes a condition and the card that reads one back cannot drift apart (FR-071).");

        foreach (var condition in HandlerStatusCatalog.Conditions)
        {
            Assert.IsFalse(
                markup.Contains(condition, StringComparison.Ordinal),
                $"The flyout markup re-lists '{condition}'; the four strings live in HandlerStatusCatalog and nowhere else (FR-102).");
            Assert.IsFalse(
                codeBehind.Contains(condition, StringComparison.Ordinal),
                $"The flyout code-behind re-lists '{condition}'; the four strings live in HandlerStatusCatalog and nowhere else (FR-102).");
        }

        var choices = XDocument.Parse(markup)
            .Descendants(s_presentation + "Button")
            .ToList();

        Assert.AreEqual(
            1,
            choices.Count,
            "The four choices must be one templated set of buttons, not one hand-written button per condition (FR-071).");
        StringAssert.Contains(
            (string?)choices[0].Attribute("Click") ?? string.Empty,
            "OnConditionChoiceClick",
            "A choice must be something the handler can choose (FR-071, FR-074).");
    }

    // ── The gate: only the row's own material handler, and only on a request they are working (FR-070,
    //    FR-074, FR-076) ───────────────────────────────────────────────────────────────────────────────

    [TestMethod]
    public void UI_WaitlistStatusFlyoutTests_Word_OpensTheFlyoutOnlyForTheRowsOwnMaterialHandler()
    {
        var ownHandler = new SampleOrder { AssignedMaterialHandler = "6331", CanCompleteOrRelease = true };
        var anotherPerson = new SampleOrder { AssignedMaterialHandler = "6331", CanCompleteOrRelease = false };
        var notTheirRequest = new SampleOrder { AssignedMaterialHandler = "9002", CanCompleteOrRelease = false };

        Assert.IsTrue(
            ownHandler.CanSetStatusCondition,
            "The row's own material handler is the one person who may open the flyout (FR-070, FR-076).");
        Assert.IsFalse(
            anotherPerson.CanSetStatusCondition,
            "The word must be inert for everyone who is not the row's material handler (FR-074).");
        Assert.IsFalse(
            notTheirRequest.CanSetStatusCondition,
            "The word must be inert on a request the signed-in person is not working (FR-074).");

        var word = StatusWordButton();
        var flyout = word
            .Descendants(s_presentation + "Flyout")
            .FirstOrDefault()
            ?? throw new AssertFailedException(
                "The status word declares no flyout host, so the picker has nowhere to open (FR-071).");

        Assert.IsTrue(
            flyout.Descendants().Any(element => element.Name.LocalName == "WaitlistStatusFlyout"),
            "The flyout host opens nothing: the conditions flyout must be the body the card's host opens (FR-071).");
    }

    [TestMethod]
    public void UI_WaitlistStatusFlyoutTests_Word_IsInertForAnotherPersonAndForARequestTheSignedInPersonIsNotWorking()
    {
        var card = Card();
        var word = StatusWordButton(card);

        StringAssert.Contains(
            (string?)word.Attribute("IsEnabled") ?? string.Empty,
            "Order.CanSetStatusCondition",
            "The word must be gated on the row's own handler, so the flyout is not merely hidden from people the store refuses (FR-070, FR-074).");

        // One route in and one only: the gated button's own flyout. Nothing else in the card may open it, and the
        // word itself may not carry a gesture that would raise it for an inert row.
        var hosts = card.Descendants(s_presentation + "Flyout").ToList();

        Assert.AreEqual(
            1,
            hosts.Count,
            "The card declares more than one flyout host, so the conditions picker could be reached without the gate (FR-074).");
        Assert.IsTrue(
            hosts[0].Ancestors().Contains(word),
            "The card's flyout host must sit on the gated status word rather than beside it (FR-070, FR-074).");
        Assert.IsFalse(
            word.Attribute("Click") is not null
                || word.Attribute("Tapped") is not null
                || word.Attribute("PointerPressed") is not null,
            "The word carries its own gesture, so an inert row could still raise the picker (FR-074).");
    }

    // ── Keyboard alone (FR-075) ───────────────────────────────────────────────────────────────────────

    [TestMethod]
    public void UI_WaitlistStatusFlyoutTests_Choices_AreReachableAndActivatableByKeyboardAlone()
    {
        var markup = FlyoutMarkup();
        var codeBehind = FlyoutCodeBehind();
        var cardCodeBehind = CardCodeBehind();

        var choices = XDocument.Parse(markup).Descendants(s_presentation + "Button").ToList();

        Assert.AreEqual(1, choices.Count, "The choices are one templated set of buttons (FR-071).");
        StringAssert.Contains(
            (string?)choices[0].Attribute("Content") ?? string.Empty,
            "Binding",
            "Each choice must read its own condition, so it is activatable by the keyboard as a button (FR-075).");

        StringAssert.Contains(
            markup,
            "KeyDown=\"OnConditionChoiceKeyDown\"",
            "Arrow movement through the choices must be handled, or a keyboard-only handler can only tab past them (FR-075).");
        StringAssert.Contains(
            codeBehind,
            "FocusManager.TryMoveFocus",
            "The choices' arrow movement must move focus itself rather than rely on a pointer (FR-075).");
        StringAssert.Contains(
            codeBehind,
            "FocusState.Programmatic",
            "The flyout must place focus on its first choice, so the keyboard path into it needs no pointer (FR-075).");

        StringAssert.Contains(
            CardMarkup(),
            "Opened=\"OnStatusConditionFlyoutOpened\"",
            "The flyout must announce that it opened, so focus can be moved into it (FR-075).");
        StringAssert.Contains(
            cardCodeBehind,
            "FocusFirstChoice()",
            "Opening the flyout must place focus on its first choice (FR-075).");
    }

    // ── The announced gate (FR-075, FR-104) ───────────────────────────────────────────────────────────

    [TestMethod]
    public void UI_WaitlistStatusFlyoutTests_Word_AnnouncesWhetherItIsOperable()
    {
        var word = StatusWordButton();

        var key = AttributeKey((string?)word.Attribute("AutomationProperties.HelpText"));

        Assert.IsTrue(
            Resources().Contains($"name=\"{key}\"", StringComparison.Ordinal),
            $"The announcement '{key}' is not a resource key, so whether the word is operable is announced in a literal (FR-075, FR-104).");
        StringAssert.Contains(
            CardCodeBehind(),
            key,
            $"The card never reads '{key}', so nothing is announced (FR-075).");

        StringAssert.Contains(
            FlyoutMarkup(),
            "AutomationProperties.Name=\"{Binding FlyoutAccessibleName, ElementName=Root}\"",
            "The flyout must carry an accessible name of its own, so it is not announced as an unlabelled container (FR-075).");
        StringAssert.Contains(
            FlyoutCodeBehind(),
            "FlyoutAccessibleName",
            "The flyout's accessible name must resolve from a resource key (FR-075, FR-104).");
    }

    // ── Choosing: the flyout closes, and the row's one command does the write (FR-074, FR-072) ─────────

    [TestMethod]
    public void UI_WaitlistStatusFlyoutTests_Choice_ClosesTheFlyoutAndHandsTheConditionToTheRow()
    {
        StringAssert.Contains(
            CardMarkup(),
            "ConditionChosen=\"OnStatusConditionChosen\"",
            "The card's host must be told which condition was chosen (FR-071, FR-074).");

        var codeBehind = CardCodeBehind();

        StringAssert.Contains(
            codeBehind,
            "StatusConditionFlyout.Hide()",
            "Choosing a condition must close the flyout (FR-074).");
        StringAssert.Contains(
            codeBehind,
            "StatusConditionChosen?.Invoke",
            "The chosen condition must be handed on with the row it belongs to (FR-070, FR-071).");

        StringAssert.Contains(
            FlyoutCodeBehind(),
            "HandlerStatusCatalog.IsCondition",
            "Only one of the four conditions may be reported, so the vocabulary stays closed all the way out (FR-071, FR-102).");
    }

    [TestMethod]
    public void UI_WaitlistStatusFlyoutTests_Choice_ReachesTheViewModelsOneWritePathWithoutASecondOne()
    {
        StringAssert.Contains(
            ListPageMarkup(),
            "StatusConditionChosen=\"OnCardStatusConditionChosen\"",
            "The list page must be told which row's condition was chosen, so the row's own command runs (FR-070, FR-076).");

        var pageCodeBehind = ListPageCodeBehind();

        StringAssert.Contains(
            pageCodeBehind,
            "ViewModel.SetStatusConditionCommand.Execute(",
            "The choice must invoke the row's command, which is the one path that writes a condition (FR-072, FR-076).");

        var writes = new[]
        {
            FlyoutCodeBehind(),
            CardCodeBehind(),
            pageCodeBehind,
        };

        foreach (var source in writes)
        {
            Assert.IsFalse(
                source.Contains("SetHandlerStatusAsync", StringComparison.Ordinal),
                "The flyout's own files name the store seam directly, which is a second write path around the row's command (FR-072, FR-076).");
        }
    }

    // ── Fixtures ─────────────────────────────────────────────────────────────────────────────────────

    private static string ControlsDirectory() => Path.Combine(
        RepositoryPatternScan.FindRepositoryRoot(),
        "Module_Waitlist",
        "Controls");

    private static string FlyoutMarkup() => File.ReadAllText(Path.Combine(ControlsDirectory(), "WaitlistStatusFlyout.xaml"));

    private static string FlyoutCodeBehind() => File.ReadAllText(Path.Combine(ControlsDirectory(), "WaitlistStatusFlyout.xaml.cs"));

    private static string CardMarkup() => File.ReadAllText(Path.Combine(ControlsDirectory(), "WaitlistLineCardView.xaml"));

    private static string CardCodeBehind() => File.ReadAllText(Path.Combine(ControlsDirectory(), "WaitlistLineCardView.xaml.cs"));

    private static string ListPageMarkup() => File.ReadAllText(Path.Combine(
        RepositoryPatternScan.FindRepositoryRoot(),
        "Module_Waitlist",
        "Views",
        "WaitlistViewPage.xaml"));

    private static string ListPageCodeBehind() => File.ReadAllText(Path.Combine(
        RepositoryPatternScan.FindRepositoryRoot(),
        "Module_Waitlist",
        "Views",
        "WaitlistViewPage.xaml.cs"));

    private static string Resources() => File.ReadAllText(Path.Combine(
        RepositoryPatternScan.FindRepositoryRoot(),
        "Strings",
        "en-us",
        "Resources.resw"));

    private static XDocument Card() => XDocument.Load(Path.Combine(ControlsDirectory(), "WaitlistLineCardView.xaml"));

    private static XElement StatusWordButton() => StatusWordButton(Card());

    private static XElement StatusWordButton(XDocument card) =>
        card
            .Descendants(s_presentation + "Button")
            .SingleOrDefault(element => (string?)element.Attribute(s_xaml + "Name") == "StatusWordButton")
        ?? throw new AssertFailedException("The card draws no status word button, so the row's own handler has no way to report a condition (FR-070).");

    /// <summary>Reads the resource key out of a localized binding such as the word's own help text.</summary>
    private static string AttributeKey(string? binding)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            binding ?? string.Empty,
            @"(?<name>[A-Za-z_]+)\s*,\s*ElementName",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        Assert.IsTrue(
            match.Success,
            $"The word's announcement does not read a value off the card: '{binding}' (FR-075).");

        var property = match.Groups["name"].Value;

        var key = System.Text.RegularExpressions.Regex.Match(
            CardCodeBehind(),
            $@"public string {property} => ""(?<key>[^""]+)"".GetLocalized\(\)",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        Assert.IsTrue(
            key.Success,
            $"'{property}' does not resolve to a resource key on the card, so the announcement is a literal (FR-104).");

        return key.Groups["key"].Value;
    }
}
