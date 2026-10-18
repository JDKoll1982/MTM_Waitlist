using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Shared;

/// <summary>
/// Every control a person operates carries an accessible name, so a screen reader announces what the control is
/// rather than "button", and the automation toolkit can reach it by name at all.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this is asserted against the markup.</b> The gap this closes was found by walking the running app with
/// <c>scripts/ui-automation/uia-surface-audit.ps1</c>, which reads the live automation tree; that walk needs a store, a sign-in and
/// a machine, so it cannot be the thing that fails a build. What it found can be: each control here had no
/// <c>AutomationProperties.Name</c> in its markup, and the reason is always structural rather than accidental - the
/// control draws its caption as a child element, so the framework has no string to derive a name from.
/// </para>
/// <para>
/// <b>The controls are named rather than counted.</b> A test that said "at least twenty controls have names" would
/// pass while the one unnamed button a person cannot operate sat in the middle of the page, so each finding is
/// pinned by its automation id. A control that is added later with the same shape is not covered by this file; the
/// live walk is what finds those, and this is what stops the fixed ones coming back.
/// </para>
/// </remarks>
[TestClass]
public sealed class AutomationSurfaceTests
{
    private static string Root => RepositoryPatternScan.FindRepositoryRoot();

    private static XDocument LoadXaml(params string[] relativeParts)
        => XDocument.Load(Path.Combine([Root, .. relativeParts]));

    private static XElement? WithAutomationId(XDocument document, string automationId)
        => document.Descendants().FirstOrDefault(element =>
            (string?)element.Attribute("AutomationProperties.AutomationId") == automationId);

    private static void AssertNamed(XDocument document, string automationId, string what)
    {
        var element = WithAutomationId(document, automationId);

        Assert.IsNotNull(element, $"{what} (automation id '{automationId}') is gone from the markup, so this guard is watching nothing.");
        Assert.IsFalse(
            string.IsNullOrWhiteSpace((string?)element!.Attribute("AutomationProperties.Name")),
            $"{what} has no accessible name, so it announces itself as its control type and cannot be reached by name "
                + "(FR-105). The caption is drawn as a child element, so the framework derives no name - the name has "
                + "to be declared.");
    }

    /// <summary>The shell's facility selector and signed-in-user badge, in both layout variants.</summary>
    /// <remarks>
    /// Both variants are checked because only one is realised at a time: the narrow layout's control is the one a
    /// walk on this machine sees, and a guard that only watched the wide one would pass while the other was broken.
    /// </remarks>
    [TestMethod]
    public void UI_AutomationSurfaceTests_TheShellsSelectorsAndUserBadgeAreNamed()
    {
        var shell = LoadXaml("Module_Core", "Views", "ShellPage.xaml");

        AssertNamed(shell, "ShellPage_FacilitySelector", "The facility selector");
        AssertNamed(shell, "ShellPage_FacilitySelectorDefault", "The facility selector (narrow layout)");
        AssertNamed(shell, "ShellPage_CurrentUserButton", "The signed-in-user badge");
        AssertNamed(shell, "ShellPage_CurrentUserButtonDefault", "The signed-in-user badge (narrow layout)");
    }

    /// <summary>The wizard's building selector and its submit button, which draws a ring beside its caption.</summary>
    [TestMethod]
    public void UI_AutomationSurfaceTests_TheWizardsBuildingSelectorAndSubmitButtonAreNamed()
    {
        AssertNamed(
            LoadXaml("Module_Waitlist", "Views", "NewRequestWorkCenterPage.xaml"),
            "NewRequestWorkCenterPage_BuildingButton",
            "The new-request building selector");

        AssertNamed(
            LoadXaml("Module_Waitlist", "Views", "NewRequestSummaryPage.xaml"),
            "NewRequestSummaryPage_SubmitButton",
            "The new-request submit button");
    }

    /// <summary>The settings controls that draw their captions as elements: the two expanders and the computers pair.</summary>
    /// <remarks>
    /// An expander whose header is a plain string lends that header to its toggle as a name; one whose header is an
    /// element lends nothing, so the toggle - the thing a person clicks - announced itself as an unnamed button.
    /// </remarks>
    [TestMethod]
    public void UI_AutomationSurfaceTests_TheSettingsExpandersAndComputerButtonsAreNamed()
    {
        var settings = LoadXaml("Module_Settings", "Views", "SettingsPage.xaml");

        AssertNamed(settings, "SettingsPage_IgnoredLocationsExpander", "The ignored-locations expander");
        AssertNamed(settings, "SettingsPage_CacheRefreshExpander", "The cache-refresh expander");
        AssertNamed(settings, "SettingsPage_Computers_AddButton", "The add-computer button");
        AssertNamed(settings, "SettingsPage_Computers_EditButton", "The edit-computer button");
    }

    /// <summary>The remembered person offered on the sign-in stage.</summary>
    /// <remarks>
    /// The choice is built from a two-line stack, so it derives no name, and it is inside a data template - which is
    /// why this one is asserted as "declares a name" rather than by id: the id is the person's sign-in name.
    /// </remarks>
    [TestMethod]
    public void UI_AutomationSurfaceTests_TheRememberedPersonChoicesDeclareAName()
    {
        var signIn = LoadXaml("Module_Startup", "Views", "SignInPage.xaml");

        var choices = signIn.Descendants()
            .Where(element => (string?)element.Attribute("AutomationProperties.AutomationId") == "{x:Bind SignInName}")
            .ToList();

        Assert.AreNotEqual(0, choices.Count, "The remembered-person choices are gone from the sign-in stage, so this guard is watching nothing.");

        foreach (var choice in choices)
        {
            Assert.IsFalse(
                string.IsNullOrWhiteSpace((string?)choice.Attribute("AutomationProperties.Name")),
                "The remembered-person choice has no accessible name, so a screen reader announces an unnamed button "
                    + "where a person's name belongs.");
        }
    }

    /// <summary>The names that are composed rather than declared say who and what they are.</summary>
    [TestMethod]
    public void Model_AutomationSurfaceTests_TheComposedNamesNameThePersonAndTheAction()
    {
        var person = new PreviousSignIn("JKOLL", "John Koll");

        StringAssert.Contains(person.ChoiceAutomationName, "John Koll", "The offered choice names the person it signs in as.");
        StringAssert.Contains(person.ChoiceAutomationName, "JKOLL", "And the sign-in name it fills in.");

        var nameless = new PreviousSignIn("JKOLL", string.Empty);
        StringAssert.Contains(nameless.ChoiceAutomationName, "JKOLL", "A person the store holds no display name for is still identifiable.");
    }
}
