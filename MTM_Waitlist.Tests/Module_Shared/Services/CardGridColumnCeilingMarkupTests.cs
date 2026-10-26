using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Shared.Services;

/// <summary>
/// Every grid in the application caps its columns at three: no screen draws a fourth card across.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why one ceiling for the whole application.</b> A grid of choices is read as a row of options, and the row stops
/// being readable as one when it grows with the monitor. The work-center screens get there through
/// <c>CardGridSizing.MaximumColumns</c>, which follows the container and stops at the ceiling; the wizard's choice
/// grids and the setup page's tile lists pin the same ceiling on their own wrap panel. Both spells the same decision,
/// so both are checked here — one that drifted to four is invisible until somebody opens the page on a wide monitor.
/// </para>
/// <para>
/// <b>The one exception is not a grid of cards.</b> <see cref="s_notCardGrids"/> names the card's own label/value
/// rows, which wrap two across inside a single card; that count describes a row of text, not a row of choices, and the
/// ceiling has nothing to say about it. The test also fails if that entry stops being true, so an exception cannot
/// outlive the markup it was written for.
/// </para>
/// </remarks>
[TestClass]
public sealed class CardGridColumnCeilingMarkupTests
{
    /// <summary>The column count every card grid caps at.</summary>
    private const string Ceiling = "3";

    /// <summary>The attribute names a column cap is declared with.</summary>
    private static readonly string[] s_capAttributes =
    [
        "MaximumRowsOrColumns",
        "CardGridSizing.MaximumColumns",
    ];

    /// <summary>Folders that hold no shipped markup: build output, tooling state and the design record.</summary>
    private static readonly string[] s_unscannedFolders = ["bin", "obj", ".git", ".vs", "TestResults", "specs", "docs", "src"];

    /// <summary>
    /// The one wrap capped at something other than three, per file, with the reason it is not a grid of cards.
    /// </summary>
    private static readonly Dictionary<string, string> s_notCardGrids = new(StringComparer.OrdinalIgnoreCase)
    {
        [@"Module_Waitlist\Views\WaitlistViewDetailPage.xaml"] =
            "the card's own label/value rows wrap two across inside one card; that is a row of text, not a row of choices",
    };

    [TestMethod]
    public void Audit_CardGridColumnCeilingMarkupTests_EveryCardGridCapsItsColumnsAtThree()
    {
        var root = RepositoryPatternScan.FindRepositoryRoot();
        var offenders = new List<string>();
        var cappedGrids = 0;
        var excepted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in XamlFiles(root))
        {
            var relative = Path.GetRelativePath(root, file);

            foreach (var (element, attribute) in ColumnCaps(XDocument.Load(file)))
            {
                cappedGrids++;

                if (s_notCardGrids.ContainsKey(relative))
                {
                    excepted.Add(relative);
                    continue;
                }

                if (!string.Equals(attribute.Value, Ceiling, StringComparison.Ordinal))
                {
                    offenders.Add(
                        $"{relative}: <{element.Name.LocalName}> caps its columns at {attribute.Value}");
                }
            }
        }

        // A traversal that found nothing would pass every assertion below, so the count is pinned first.
        Assert.IsTrue(
            cappedGrids >= 8,
            $"Only {cappedGrids} grids declare a column cap, so this audit is no longer reading the markup it was written for.");

        Assert.AreEqual(
            0,
            offenders.Count,
            "These grids draw more than three cards across, which turns a row of choices into a wall:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, offenders));

        CollectionAssert.AreEquivalent(
            s_notCardGrids.Keys.ToArray(),
            excepted.ToArray(),
            "An excepted file no longer declares a column cap, so its entry in this audit is stale and should be deleted.");
    }

    /// <summary>The column-cap attributes under one document, with the element each was declared on.</summary>
    /// <param name="document">The markup to read.</param>
    private static IEnumerable<(XElement Element, XAttribute Attribute)> ColumnCaps(XDocument document) =>
        document
            .Descendants()
            .SelectMany(element => element.Attributes()
                .Where(attribute => s_capAttributes.Contains(attribute.Name.LocalName, StringComparer.Ordinal))
                .Select(attribute => (Element: element, Attribute: attribute)));

    /// <summary>Every shipped XAML file: the markup a person actually sees, with no build output and no design record.</summary>
    /// <param name="root">The repository root.</param>
    private static IEnumerable<string> XamlFiles(string root) =>
        Directory
            .EnumerateFiles(root, "*.xaml", SearchOption.AllDirectories)
            .Where(file => !Path.GetRelativePath(root, file)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(folder => s_unscannedFolders.Contains(folder, StringComparer.OrdinalIgnoreCase)))
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase);
}
