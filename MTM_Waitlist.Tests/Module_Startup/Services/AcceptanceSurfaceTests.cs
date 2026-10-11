using System.Reflection;
using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The cases the sweeps found are recorded, and every one is either covered by a test or recorded as no longer
/// applying with its reason (FR-031, SC-013).
/// </summary>
/// <remarks>
/// <para>
/// A case that is silently missing is the failure this guards against, so the check is an accounting rather than a
/// sample: every row in the catalogue has to say what became of it, and every test a row cites has to exist. A row
/// pointing at a test that was renamed is worse than no row, because it reads as coverage.
/// </para>
/// <para>
/// <b>What is not asserted.</b> That a cited test passes, and that a cited test is the right test for the case.
/// The suite's own run answers the first, and the second is a judgement a person makes; this class keeps the
/// catalogue honest about which tests it claims.
/// </para>
/// </remarks>
[TestClass]
public sealed class AcceptanceSurfaceTests
{
    private static readonly Regex s_tableRow = new(
        @"^\|\s*(?<number>\d+)\s*\|\s*(?<case>.+?)\s*\|\s*(?<group>\d+)\s*\|\s*(?<disposition>.+?)\s*\|\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex s_backticked = new(
        @"`(?<token>[^`]+)`",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static string Root => RepositoryPatternScan.FindRepositoryRoot();

    private static Assembly TestAssembly => typeof(AcceptanceSurfaceTests).Assembly;

    /// <summary>
    /// Every case in the catalogue carries a disposition, so none was dropped without a reason (FR-031).
    /// </summary>
    [TestMethod]
    public void Service_AcceptanceSurfaceTests_EveryCataloguedCase_CarriesADisposition()
    {
        var rows = CatalogueRows();

        Assert.IsTrue(
            rows.Count >= 20,
            $"Only {rows.Count} cases were read from the catalogue, so a clean result here would prove nothing.");

        var undisposed = rows
            .Where(row => row.Disposition.Length == 0)
            .Select(row => row.Number)
            .ToList();

        Assert.AreEqual(
            0,
            undisposed.Count,
            "These catalogue cases say nothing about what became of them, which is the silent drop FR-031 forbids: "
                + string.Join(", ", undisposed));
    }

    /// <summary>
    /// A case is covered, or it is recorded as no longer applying with its reason — never merely "read". The one
    /// exception is stated in the catalogue as read-verified and is named here so it cannot grow quietly (FR-031).
    /// </summary>
    [TestMethod]
    public void Service_AcceptanceSurfaceTests_EveryDisposition_IsCoverageOrAStatedReason()
    {
        string[] reasonMarkers = ["No longer applies", "Retired", "Read-verified", "Read-verified only"];

        var unexplained = CatalogueRows()
            .Where(row => !row.Disposition.StartsWith("Covered", StringComparison.OrdinalIgnoreCase))
            .Where(row => !reasonMarkers.Any(marker => row.Disposition.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            .Select(row => $"{row.Number}: {row.Disposition}")
            .ToList();

        Assert.AreEqual(
            0,
            unexplained.Count,
            "These cases are neither covered nor given a reason, so a reader cannot tell a gap from a decision:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, unexplained));
    }

    /// <summary>
    /// Every test the catalogue cites exists. A row pointing at a renamed or deleted test reads as coverage while
    /// proving nothing (FR-031, SC-013).
    /// </summary>
    /// <remarks>
    /// Only the citation clause is read: a row may carry a dated note explaining a correction, and a note that
    /// names the citation it replaced must not be mistaken for a citation itself.
    /// </remarks>
    [TestMethod]
    public void Service_AcceptanceSurfaceTests_EveryCitedTest_Exists()
    {
        var missing = new List<string>();

        foreach (var row in CatalogueRows())
        {
            if (!row.Disposition.StartsWith("Covered", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (Match match in s_backticked.Matches(CitationClause(row.Disposition)))
            {
                var token = match.Groups["token"].Value.Trim();

                if (!Resolves(token))
                {
                    missing.Add($"case {row.Number} cites '{token}'");
                }
            }
        }

        Assert.AreEqual(
            0,
            missing.Count,
            "The catalogue cites tests that no longer exist, so it claims coverage it does not have:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, missing));
    }

    /// <summary>
    /// The part of a disposition that cites tests, with any dated note that follows it removed.
    /// </summary>
    private static string CitationClause(string disposition)
    {
        string[] noteMarkers = ["Corrected ", "Restated ", "Added ", "Placed ", "No longer applies"];

        var end = disposition.Length;

        foreach (var marker in noteMarkers)
        {
            var index = disposition.IndexOf(marker, StringComparison.Ordinal);

            if (index >= 0 && index < end)
            {
                end = index;
            }
        }

        return disposition[..end];
    }

    /// <summary>
    /// The catalogue and the specification hold the same cases, so a case added to one and not the other is caught
    /// here rather than being discovered as a gap (FR-031, SC-013).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two lists are matched on the order the specification states them in, which is the mapping
    /// <c>STARTUP-REBUILD-BRIEF.md</c> S13.1 declares: its rows 1 to 24 are the specification's twenty-four edge
    /// cases in its own order, and any further row is a case the specification no longer lists and that the
    /// catalogue keeps as the record of a withdrawal. The catalogue may therefore be longer than the
    /// specification, and must never be shorter.
    /// </para>
    /// <para>
    /// Each pair is recognised by a keyword that has to appear in both, rather than by comparing the two texts.
    /// The catalogue deliberately paraphrases and sometimes adds what a case taught it — "so setup returns" — so
    /// demanding the same words would fail on a record that is better than the specification, and a test that
    /// fails on a good record is one that gets edited until it passes.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void Service_AcceptanceSurfaceTests_TheCatalogue_AccountsForEveryEdgeCaseTheSpecificationStates()
    {
        // One keyword per specification edge case, in its order, each of which must appear in that case and in the
        // catalogue row that answers for it. Where the catalogue paraphrases, the keyword is the phrase the two do
        // share rather than a reworded copy of either.
        string[] keywords =
        [
            "cannot be reached at launch",
            "share holding pictures cannot be reached",
            "configuration is deleted while",
            "own name",
            "location is left blank",
            "signed in last",
            "temporary credential",
            "launch window is closed",
            "hardware address",
            "sign-out whose restart fails",
            "two copies",
            "answers in seconds",
            "takes longer than the launch allows",
            "signs out at a shared computer",
            "change how long a session lasts",
            "saved size cannot be applied",
            "legacy temporary value",
            "while the person is signed in elsewhere",
            "store's ceiling",
            "fault happens while the application is already recording",
            "aggregate fault",
            "message contains a credential",
            "store refuses the write",
            "clipboard refuses a copy",
        ];

        var specCases = SpecificationEdgeCases();
        var catalogueCases = CatalogueCases();

        Assert.AreEqual(
            keywords.Length,
            specCases.Count,
            $"The specification states {specCases.Count} edge cases and this check knows {keywords.Length} of them, so a case was added or removed without the acceptance surface being updated (FR-031).");

        Assert.IsTrue(
            catalogueCases.Count >= specCases.Count,
            $"The specification states {specCases.Count} edge cases and the catalogue accounts for {catalogueCases.Count}, so {specCases.Count - catalogueCases.Count} case(s) were dropped without a reason (FR-031).");

        for (var index = 0; index < specCases.Count; index++)
        {
            var keyword = keywords[index];

            Assert.IsTrue(
                Reads(specCases[index], keyword),
                $"The specification's edge case {index + 1} no longer reads as the keyword this check matches it on ('{keyword}')."
                    + Environment.NewLine
                    + $"  specification: {specCases[index]}");

            Assert.IsTrue(
                Reads(catalogueCases[index], keyword),
                $"Catalogue row {index + 1} no longer answers for the specification's edge case {index + 1}."
                    + Environment.NewLine
                    + $"  specification: {specCases[index]}"
                    + Environment.NewLine
                    + $"  catalogue:     {catalogueCases[index]}");
        }
    }

    /// <summary>
    /// Whether a case reads as a keyword, treating a hyphen as a space: the specification writes
    /// "temporary credential" where the catalogue writes "temporary-credential", and the two say the same thing.
    /// </summary>
    private static bool Reads(string text, string keyword) => text
        .Replace('-', ' ')
        .Contains(keyword.Replace('-', ' '), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The catalogue states the gap in its own record rather than claiming the sweeps' original breakdown was
    /// recovered. An unstated gap is what turns a record into a claim (FR-031).
    /// </summary>
    [TestMethod]
    public void Service_AcceptanceSurfaceTests_TheCatalogue_StatesThatTheOriginalSweepBreakdownWasNotRecovered()
    {
        var brief = File.ReadAllText(Path.Combine(Root, "STARTUP-REBUILD-BRIEF.md"));

        StringAssert.Contains(
            brief,
            "is not recovered",
            "The catalogue no longer states that the sweeps' original breakdown was not recovered, so a partial record would read as a complete one (FR-031).");
    }

    /// <summary>
    /// The cases recorded as no longer applying say why, one reason each (FR-031).
    /// </summary>
    [TestMethod]
    public void Service_AcceptanceSurfaceTests_TheRetiredCases_EachStateWhyTheyNoLongerApply()
    {
        var brief = File.ReadAllText(Path.Combine(Root, "STARTUP-REBUILD-BRIEF.md"));

        StringAssert.Contains(
            brief,
            "### S13.2 Cases that no longer apply",
            "The section recording why cases stopped applying is gone, so those cases are now unexplained (FR-031).");

        var section = brief[brief.IndexOf("### S13.2 Cases that no longer apply", StringComparison.Ordinal)..];

        var reasoned = section
            .Split('\n')
            .Count(line => line.StartsWith("| ", StringComparison.Ordinal) && line.Contains('|', StringComparison.Ordinal) && line.Split('|').Length >= 4);

        Assert.IsTrue(
            reasoned >= 3,
            $"The section recording why cases stopped applying holds {reasoned} rows, so the reasons have been dropped (FR-031).");
    }

    private static bool Resolves(string token)
    {
        // A row cites either "Type" or "Type.Method". A bare token is resolved as a type first, then as a method,
        // because two rows cite a bare method name whose type is obvious from the name itself.
        var separator = token.IndexOf('.', StringComparison.Ordinal);

        if (separator > 0)
        {
            var typeName = token[..separator];
            var memberName = token[(separator + 1)..];

            var type = FindType(typeName);

            return type is not null
                && type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Any(method => method.Name == memberName);
        }

        return FindType(token) is not null
            || TestAssembly
                .GetTypes()
                .Any(type => type
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Any(method => method.Name == token));
    }

    private static Type? FindType(string name) => TestAssembly
        .GetTypes()
        .FirstOrDefault(type => type.Name == name);

    private static List<(int Number, string Case, string Disposition)> CatalogueRows()
    {
        var brief = File.ReadAllText(Path.Combine(Root, "STARTUP-REBUILD-BRIEF.md"));

        var start = brief.IndexOf("### S13.1 The walkable catalogue", StringComparison.Ordinal);

        Assert.IsTrue(start >= 0, "The walkable catalogue is gone from the brief, so there is no acceptance surface to account for.");

        var end = brief.IndexOf("### S13.2", start, StringComparison.Ordinal);

        var section = end > start ? brief[start..end] : brief[start..];

        var rows = new List<(int, string, string)>();

        foreach (var line in section.Split('\n'))
        {
            var match = s_tableRow.Match(line.TrimEnd('\r'));

            if (!match.Success)
            {
                continue;
            }

            rows.Add((
                int.Parse(match.Groups["number"].Value, System.Globalization.CultureInfo.InvariantCulture),
                match.Groups["case"].Value,
                match.Groups["disposition"].Value));
        }

        return rows;
    }

    private static List<string> CatalogueCases() => CatalogueRows()
        .Select(row => row.Case)
        .ToList();

    private static List<string> SpecificationEdgeCases()
    {
        var spec = File.ReadAllText(Path.Combine(Root, "specs", "010-startup-rebuild", "spec.md"));

        var start = spec.IndexOf("## Edge Cases", StringComparison.Ordinal);

        Assert.IsTrue(start >= 0, "The specification no longer states its edge cases, so the acceptance surface has no source.");

        var end = spec.IndexOf("\n## ", start + 4, StringComparison.Ordinal);

        var section = end > start ? spec[start..end] : spec[start..];

        return section
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("- ", StringComparison.Ordinal))
            // The case's name is what the catalogue rows carry; the clause after the colon is the specification's
            // explanation of it, which a row states again in its own words as the disposition.
            .Select(line => line[2..].Split(':', '—')[0].Trim())
            .ToList();
    }
}
