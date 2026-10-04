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

    /// <summary>The XAML language namespace, which is where <c>x:Load</c> and <c>x:Name</c> live.</summary>
    private static readonly XNamespace s_xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

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

    /// <summary>
    /// The developer log panel holds wrapping text and a scrolling list, so it may only be placed where it is
    /// given a bounded width to wrap into.
    /// </summary>
    /// <remarks>
    /// Observed 2026-10-02: opening the Settings page ended the process with a
    /// <c>Microsoft.UI.Xaml.LayoutCycleException</c>, and the debug trace named a <c>Measure(infx220)</c> and a
    /// <c>Measure(infx160)</c> <c>ScrollContentPresenter</c> holding a 6733-pixel-wide <c>TextBlock</c> — the
    /// panel's two max-height viewers, each laid out as a single line. The panel was the
    /// <c>SettingsExpander</c>'s own content, and a card's content slot aligns to the right by default, which
    /// sizes that column to the content and so measures it with an unbounded width. Hosting the panel in a
    /// vertically-aligned <c>SettingsCard</c> gives it the card's own width instead.
    /// </remarks>
    [TestMethod]
    public void TheDeveloperLogPanel_IsHostedWhereItsWrappingTextGetsABoundedWidth()
    {
        var panel = XDocument.Parse(PageMarkup())
            .Descendants()
            .SingleOrDefault(element => element.Name.LocalName == "DeveloperLogPanelView");

        Assert.IsNotNull(panel, "The settings page no longer hosts the developer log panel.");

        var host = panel.Parent;
        Assert.IsNotNull(host, "The developer log panel has no host element.");

        Assert.AreEqual(
            "SettingsCard",
            host!.Name.LocalName,
            "The panel must be hosted in a card that gives it a width of its own. An expander's own content "
                + "slot, and a card's right-aligned content slot, both measure what they hold with an unbounded "
                + "width, and the panel's wrapping entry text then asks to be thousands of pixels wide.");

        Assert.AreEqual(
            "Vertical",
            host.Attribute("ContentAlignment")?.Value,
            "The hosting card must align its content vertically (the card's own documented pairing with "
                + "HorizontalContentAlignment=Stretch), because that is what hands the panel the card's width.");
    }

    /// <summary>
    /// The panel puts no scroll viewer of its own anywhere, so nothing it draws is ever measured against a
    /// viewport.
    /// </summary>
    /// <remarks>
    /// This is the shape of the redesign that followed the 2026-10-02 crash. Each entry used to keep its long
    /// exception chain and payload in two bounded, individually scrolling boxes; each of those was measured as
    /// <c>Measure(infx220)</c> and <c>Measure(infx160)</c> and laid its wrapping TextBlock out as a single
    /// 6733-pixel line, which is what ended the process. The detail now lives in an <c>Expander</c> whose content
    /// wraps and makes the entry taller, and the list itself does the scrolling.
    /// </remarks>
    [TestMethod]
    public void TheDeveloperLogPanel_PutsNoScrollerInsideAnEntry()
    {
        var document = XDocument.Load(PanelPath());

        var viewers = document.Descendants().Where(element => element.Name.LocalName == "ScrollViewer").ToList();

        Assert.AreEqual(
            0,
            viewers.Count,
            "The panel must hold no scroll viewer of its own: a viewport measures what it holds with an unbounded "
                + "width, and the entry text it holds then asks to be thousands of pixels wide.");

        Assert.IsTrue(
            document.Descendants().Any(element => element.Name.LocalName == "Expander"),
            "The detail that needs a boundary is offered in an Expander, whose content wraps and grows downwards.");
    }

    /// <summary>
    /// Both of the panel's lists are bounded in height, never scroll sideways, and stretch their entries, so an
    /// entry is always measured against the list's own width.
    /// </summary>
    [TestMethod]
    public void TheDeveloperLogPanel_ListsAreBoundedAndNeverScrollSideways()
    {
        var document = XDocument.Load(PanelPath());

        foreach (var id in new[] { "DeveloperLogPanelView_Entries", "DeveloperLogPanelView_Groups" })
        {
            var list = document.Descendants().SingleOrDefault(element =>
                element.Attribute("AutomationProperties.AutomationId")?.Value == id);

            Assert.IsNotNull(list, $"The panel's '{id}' list was not found.");

            Assert.AreEqual(
                "Disabled",
                list!.Attribute("ScrollViewer.HorizontalScrollMode")?.Value,
                $"'{id}' must switch horizontal scrolling off: the scroll mode is what permits the unbounded "
                    + "measure, and it is Enabled by default.");

            Assert.IsNotNull(
                list.Attribute("MaxHeight"),
                $"'{id}' must bound its own height, so that it scrolls itself rather than asking the page for an "
                    + "unbounded height and realizing every entry it holds.");

            var containerStyle = list.Elements().SingleOrDefault(element => element.Name.LocalName == "ListView.ItemContainerStyle");
            Assert.IsNotNull(containerStyle, $"'{id}' must carry the item container style that stretches its entries.");
            Assert.IsTrue(
                containerStyle!.Descendants().Any(element =>
                    element.Name.LocalName == "Setter" && element.Attribute("Value")?.Value == "Stretch"),
                $"Each entry of '{id}' is stretched to the list's own width, so the entry wraps inside it.");
        }
    }

    /// <summary>
    /// Every text block in the panel whose text comes out of the store wraps, so none of them can ask its host for
    /// the width of its own content.
    /// </summary>
    /// <remarks>
    /// A short, fixed, shipped label may sit at its natural width. A value read out of the log store may not: a
    /// stored exception chain runs to thousands of pixels when it is not allowed to wrap.
    /// </remarks>
    [TestMethod]
    public void TheDeveloperLogPanel_WrapsEveryBoundTextBlock()
    {
        var document = XDocument.Load(PanelPath());

        var unwrapped = document
            .Descendants()
            .Where(element => element.Name.LocalName == "TextBlock")
            .Where(element =>
            {
                var text = element.Attribute("Text")?.Value;
                return text is not null
                    && (text.Contains("{x:Bind", StringComparison.Ordinal) || text.Contains("{Binding", StringComparison.Ordinal));
            })
            .Where(element => element.Attribute("TextWrapping")?.Value != "Wrap")
            .Select(element => element.Attribute("Text")?.Value ?? string.Empty)
            .ToList();

        Assert.AreEqual(
            0,
            unwrapped.Count,
            "These text blocks take their text from the store without wrapping, so each one asks its host for the "
                + "width of its own content:" + Environment.NewLine + string.Join(Environment.NewLine, unwrapped));
    }

    /// <summary>The path to the developer log panel's markup.</summary>
    private static string PanelPath()
    {
        var path = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Settings",
            "Views",
            "DeveloperLogPanelView.xaml");

        Assert.IsTrue(File.Exists(path), $"The log panel markup was not found at '{path}'.");

        return path;
    }

    /// <summary>
    /// A settings card that holds content able to grow sideways must align that content vertically.
    /// </summary>
    /// <remarks>
    /// This is the shape that ended the process on 2026-10-02 (see the developer-log-panel test above): a
    /// right-aligned content slot measures what it holds with an unbounded width, so a list hands that width on
    /// to its entries, and a wrapping text block with no maximum of its own lays itself out as a single very
    /// long line. Either way the card grows to match it and the layout engine gives up. A control hosted from
    /// another file is not followed here - the panel case is pinned by its own test - so this scan covers what
    /// is written inline.
    /// </remarks>
    [TestMethod]
    public void SettingsCardsHoldingContentThatCanGrow_AlignItVertically()
    {
        string[] scrollingControls = ["ListView", "GridView", "ItemsControl", "ItemsRepeater", "ScrollViewer"];
        var violations = new List<string>();

        foreach (var file in RepositoryPatternScan.EnumerateScannedFiles(
            RepositoryPatternScan.FindRepositoryRoot(),
            new RepositoryScanScope { Extensions = [".xaml"] }))
        {
            var document = XDocument.Load(file);
            var relativePath = Path.GetRelativePath(RepositoryPatternScan.FindRepositoryRoot(), file);

            foreach (var card in document.Descendants().Where(element =>
                element.Name.LocalName is "SettingsCard" or "SettingsExpander"))
            {
                if (card.Attribute("ContentAlignment")?.Value == "Vertical")
                {
                    continue;
                }

                foreach (var content in card.Elements().Where(element => !element.Name.LocalName.Contains('.')))
                {
                    var growth = content
                        .DescendantsAndSelf()
                        .Select(element => element.Name.LocalName)
                        .FirstOrDefault(name => scrollingControls.Contains(name, StringComparer.Ordinal))
                        ?? content
                            .Descendants()
                            .FirstOrDefault(element =>
                                element.Name.LocalName == "TextBlock"
                                && element.Attribute("TextWrapping")?.Value == "Wrap"
                                && element.Attribute("MaxWidth") is null)
                            ?.Name.LocalName;

                    if (growth is not null)
                    {
                        violations.Add($"{relativePath}: <{card.Name.LocalName}> holds <{content.Name.LocalName}> containing <{growth}>");
                    }
                }
            }
        }

        Assert.AreEqual(
            0,
            violations.Count,
            "A card that holds a list, a scroll viewer, or wrapping text with no maximum width, must align that "
                + "content vertically: right alignment measures it with an unbounded width, which is what a "
                + "wrapping entry then grows into until the layout engine gives up:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
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
    /// No element inside a <c>SettingsExpander.Items</c> collection may carry <c>x:Load</c>.
    /// </summary>
    /// <remarks>
    /// That collection is not in the page's namescope, and the code a <c>x:Load</c> binding generates resolves the
    /// element with <c>FindName</c> — which answers null there and terminates the process while the page is being
    /// built. Observed 2026-09-24: opening the Settings page closed the application, and the only record was
    /// <c>.NET Runtime</c> in the event log naming
    /// <c>SettingsPage_obj1_Bindings.Update_ViewModel_…</c> → <c>FrameworkElement.FindName</c>. Hide the element
    /// with a <c>Visibility</c> binding instead: that is a property binding and needs no name lookup.
    /// </remarks>
    [TestMethod]
    public void NoElementInsideASettingsExpanderItems_CarriesXLoad()
    {
        var violations = new List<string>();

        foreach (var file in RepositoryPatternScan.EnumerateScannedFiles(
            RepositoryPatternScan.FindRepositoryRoot(),
            new RepositoryScanScope { Extensions = [".xaml"] }))
        {
            var document = XDocument.Load(file);

            violations.AddRange(document
                .Descendants()
                .Where(element => element.Name.LocalName == "SettingsExpander.Items")
                .SelectMany(items => items.Descendants())
                .Where(element => element.Attribute(s_xaml + "Load") is not null)
                .Select(element => $"{Path.GetRelativePath(RepositoryPatternScan.FindRepositoryRoot(), file)}: <{element.Name.LocalName}>"));
        }

        Assert.AreEqual(
            0,
            violations.Count,
            "An x:Load inside a SettingsExpander's item collection cannot be resolved by name, and the page that "
                + "declares it closes the application when it opens:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
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
