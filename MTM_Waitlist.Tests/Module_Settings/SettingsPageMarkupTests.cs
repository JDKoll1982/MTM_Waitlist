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
