using System.Text.RegularExpressions;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Settings;

/// <summary>
/// US3 check (<c>contracts/verification-gates.md</c> G4 #6, contract C4). An <c>x:Uid</c> maps a whole
/// element to a resource group, so repeating one makes unrelated labels inherit the same text — and a
/// <c>x:Uid</c> with no entry of its own leaves the element without its localized text.
/// </summary>
[TestClass]
public sealed class SettingsPageMarkupTests
{
    private static readonly Regex s_xUid = new(@"x:Uid=""(?<uid>[^""]+)""", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [TestMethod]
    public void SettingsPage_DeclaresEachXUidAtMostOnce()
    {
        var duplicates = DeclaredUids()
            .GroupBy(uid => uid, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key} x{group.Count()}")
            .ToList();

        Assert.AreEqual(
            0,
            duplicates.Count,
            "A repeated x:Uid gives unrelated elements the same localized text:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, duplicates));
    }

    [TestMethod]
    public void SettingsPage_ResolvesEveryXUidToItsOwnResourceEntry()
    {
        var resourceKeys = ResourceKeys();

        Assert.IsTrue(resourceKeys.Count > 100, $"Only {resourceKeys.Count} resource keys were found, so a clean result would prove nothing.");

        var unresolved = DeclaredUids()
            .Distinct(StringComparer.Ordinal)
            .Where(uid => !resourceKeys.Any(key => key.StartsWith(uid + ".", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Assert.AreEqual(
            0,
            unresolved.Count,
            "These x:Uid values have no resource entry of their own, so their element carries no localized text:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, unresolved));
    }

    /// <summary>
    /// US5 (FR-018, FR-020, FR-021). The minutes panel shows the Item's <b>configured</b> minutes and the
    /// <b>observed</b> average as two separate values, the editor is bound to that panel's own gate, and the
    /// picture entry point opens the <b>Item-keyed</b> dialog — not the retired subtype one.
    /// </summary>
    [TestMethod]
    public void SettingsPage_ShowsTheConfiguredAndObservedValuesSeparately_AndKeepsTheMinutesGate()
    {
        var markup = PageMarkup();

        StringAssert.Contains(markup, "{Binding DisplayName}", "The minutes row names the Item.");
        StringAssert.Contains(markup, "{Binding Minutes, Mode=TwoWay", "The editable column is the configured figure.");
        StringAssert.Contains(markup, "{Binding ConfiguredValueLabel}", "The configured/default marker is rendered.");
        StringAssert.Contains(markup, "{Binding ObservedAverageText}", "The observed average is rendered beside it, not instead of it.");
        StringAssert.Contains(
            markup,
            "UrgencyAllotments.CanManageUrgencySettings",
            "The editor is bound to the minutes screen's own role gate (FR-020).");

        Assert.IsFalse(
            markup.Contains("SubtypeName", StringComparison.Ordinal),
            "The row is keyed by Item; the retired subtype vocabulary is not a binding source.");
    }

    [TestMethod]
    public void SettingsPage_OpensTheItemKeyedPictureScreen()
    {
        var markup = PageMarkup();

        StringAssert.Contains(markup, "Click=\"RequestItemImages_Click\"");
        Assert.IsFalse(
            markup.Contains("RequestSubtypeImages_Click", StringComparison.Ordinal),
            "The subtype entry point is renamed and re-pointed, not duplicated.");

        var codeBehindPath = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Settings",
            "Views",
            "SettingsPage.xaml.cs");
        Assert.IsTrue(File.Exists(codeBehindPath), $"The settings code-behind was not found at '{codeBehindPath}'.");

        var codeBehind = File.ReadAllText(codeBehindPath);
        StringAssert.Contains(codeBehind, "new RequestItemImagesDialog(");
        StringAssert.Contains(codeBehind, "App.GetService<RequestItemImagesDialogViewModel>()");
    }

    /// <summary>
    /// US5 (FR-021, FR-023). The two dunnage lists are the shared card carrying each type's picture, and showing
    /// or hiding a type is still the card's own action — the conversion adds the picture without taking the
    /// action away.
    /// </summary>
    [TestMethod]
    public void TheTwoDunnageLists_AreCardsThatStillShowAndHideTheirType()
    {
        var markup = PageMarkup();

        StringAssert.Contains(markup, "AutomationProperties.AutomationId=\"SettingsPage_VisibleDunnageTypesList\"");
        StringAssert.Contains(markup, "AutomationProperties.AutomationId=\"SettingsPage_HiddenDunnageTypesList\"");

        // Three occurrences in all: one per dunnage list, and the same template the converted part list uses.
        var cardUses = markup.Split("ContentTemplate=\"{StaticResource PartPictureCardTemplate}\"", StringSplitOptions.None).Length - 1;
        Assert.IsTrue(
            cardUses >= 2,
            $"Both dunnage lists draw the shared card carrying each type's picture; found {cardUses} use(s).");

        StringAssert.Contains(markup, "Content=\"{Binding}\"", "The card is handed the whole type, so its picture and its name come from it.");
        StringAssert.Contains(markup, "ViewModel.HideDunnageTypeCommand", "Hiding a type is still the visible list's action.");
        StringAssert.Contains(markup, "ViewModel.ShowDunnageTypeCommand", "Showing a type is still the hidden list's action.");
    }

    private static string PageMarkup()
    {
        var path = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Settings",
            "Views",
            "SettingsPage.xaml");

        Assert.IsTrue(File.Exists(path), $"The settings markup was not found at '{path}'.");

        return File.ReadAllText(path);
    }

    /// <summary>
    /// US1 (T046, FR-080, FR-082, FR-084, SC-020). The Administration category's two entries are cards that
    /// navigate, not expanders, each loaded lazily with its own name, each carrying its own title from its own
    /// resource key, and neither shown to a reader who cannot use it.
    /// </summary>
    [TestMethod]
    public void TheAdministrationEntries_NavigateAndCarryTheirOwnLabels()
    {
        var markup = PageMarkup();

        foreach (var entry in new[] { "UserManagementEntryCard", "PermissionsEntryCard" })
        {
            var card = ElementWithName(markup, entry);

            Assert.IsTrue(
                card.StartsWith("<wct:SettingsCard", StringComparison.Ordinal),
                $"'{entry}' must be a card that navigates rather than an expander that unfolds (FR-082).");
            StringAssert.Contains(card, "x:Load=", $"'{entry}' is loaded only for a reader who may use it.");
            StringAssert.Contains(card, "IsClickEnabled=\"True\"", $"'{entry}' is clicked, not expanded.");
            StringAssert.Contains(card, "Command=", $"'{entry}' navigates through a command.");
            StringAssert.Contains(card, "AutomationProperties.Name=", $"'{entry}' announces what it opens.");
        }

        // Neither entry is an expander, and the category itself is not one either.
        Assert.IsFalse(
            ElementWithName(markup, "AdministrationCategoryPanel").Contains("SettingsExpander", StringComparison.Ordinal),
            "The Administration category holds entries that navigate, so nothing in it expands.");

        var titles = ResourceValues(["Administration_Users.Title", "Administration_Permissions.Title"]);
        Assert.AreEqual(2, titles.Count, "Both entries must have a title of their own.");
        Assert.AreNotEqual(
            titles[0],
            titles[1],
            "No two labels may share a key or a value, so the two entries do not read as the same thing (SC-020).");
    }

    /// <summary>The one element carrying <paramref name="xName"/>, from its opening tag to its matching close.</summary>
    private static string ElementWithName(string markup, string xName)
    {
        var start = markup.IndexOf($"x:Name=\"{xName}\"", StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"'{xName}' was not found in the settings markup.");

        var tagStart = markup.LastIndexOf('<', start);
        var tagEnd = markup.IndexOf('>', start);

        // A self-closing element ends at its own '>'; otherwise take the element's own closing tag.
        if (markup[tagEnd - 1] == '/')
        {
            return markup[tagStart..(tagEnd + 1)];
        }

        var openingTag = markup[tagStart..(tagEnd + 1)];
        var elementName = openingTag.TrimStart('<');
        var nameEnd = elementName.IndexOfAny([' ', '>', '\r', '\n']);
        elementName = nameEnd < 0 ? elementName : elementName[..nameEnd];

        var closing = markup.IndexOf($"</{elementName}>", tagEnd, StringComparison.Ordinal);
        Assert.IsTrue(closing > tagEnd, $"'{xName}' has no closing tag, so it is not a well-formed element.");

        return markup[tagStart..(closing + elementName.Length + 3)];
    }

    /// <summary>The shipped values of the given resource keys, read from the resource files.</summary>
    private static List<string> ResourceValues(IReadOnlyList<string> resourceKeys)
    {
        var values = new List<string>();
        var scope = new RepositoryScanScope { Extensions = [".resw"] };

        foreach (var file in RepositoryPatternScan.EnumerateScannedFiles(RepositoryPatternScan.FindRepositoryRoot(), scope))
        {
            foreach (var data in XDocument.Load(file).Descendants("data"))
            {
                var name = data.Attribute("name")?.Value;
                if (name is not null && resourceKeys.Contains(name, StringComparer.Ordinal))
                {
                    values.Add(data.Element("value")?.Value ?? string.Empty);
                }
            }
        }

        return values;
    }

    private static IReadOnlyList<string> DeclaredUids()
    {
        var path = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Settings",
            "Views",
            "SettingsPage.xaml");

        Assert.IsTrue(File.Exists(path), $"The settings markup was not found at '{path}'.");

        var markup = File.ReadAllText(path);

        return [.. s_xUid.Matches(markup).Select(match => match.Groups["uid"].Value)];
    }

    /// <summary>Every resource key shipped anywhere, so an x:Uid may resolve in any resource file.</summary>
    private static IReadOnlyList<string> ResourceKeys()
    {
        var scope = new RepositoryScanScope { Extensions = [".resw"] };
        var keys = new List<string>();

        foreach (var file in RepositoryPatternScan.EnumerateScannedFiles(RepositoryPatternScan.FindRepositoryRoot(), scope))
        {
            foreach (var data in XDocument.Load(file).Descendants("data"))
            {
                var name = data.Attribute("name")?.Value;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    keys.Add(name);
                }
            }
        }

        return keys;
    }
}
