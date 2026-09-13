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
