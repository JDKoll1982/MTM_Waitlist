using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Settings;

/// <summary>
/// The roster page's shape (T045, FR-083 to FR-096). The suite cannot render XAML, so what is asserted here is the
/// set of rules the markup itself has to keep: which five facts are columns, that a row's only job is to open its
/// person, that no column carries a fixed width, and that every string on the page resolves to a shipped resource.
/// </summary>
/// <remarks>
/// The owner narrowed this page deliberately, and each narrowing is pinned here rather than left to review: no
/// email, joined date or two-factor column because this system holds none of them; no per-row edit or delete
/// because deactivation is the reversible act and the person's own page is where it happens; and no bulk
/// selection or export.
/// </remarks>
[TestClass]
public sealed class UserManagementPageMarkupTests
{
    private const string PagePath = "Module_Settings/Views/UserManagementPage.xaml";
    private const string CodeBehindPath = "Module_Settings/Views/UserManagementPage.xaml.cs";

    [TestMethod]
    public void TheTable_ShowsTheFiveFactsThisSystemHolds_AndNothingItDoesNot()
    {
        var markup = PageMarkup();

        StringAssert.Contains(markup, "IsItemClickEnabled=\"True\"", "The roster is a table whose every row can be reached.");

        var facts = new[] { "CommandParameter=\"Name\"", "CommandParameter=\"SignInName\"", "CommandParameter=\"EmployeeNumber\"", "CommandParameter=\"Role\"", "CommandParameter=\"Status\"" };
        var positions = facts.Select(fact => markup.IndexOf(fact, StringComparison.Ordinal)).ToArray();

        Assert.IsFalse(
            positions.Any(position => position < 0),
            $"Every fact this system holds is a column; missing: {string.Join(", ", facts.Where((_, index) => positions[index] < 0))}.");
        CollectionAssert.AreEqual(
            positions.OrderBy(position => position).ToArray(),
            positions,
            "The columns run in the order the five facts are stated.");
        Assert.AreEqual(
            facts.Length,
            Regex.Matches(markup, "CommandParameter=\"", RegexOptions.CultureInvariant).Count,
            "The table has these five columns and no sixth.");

        foreach (var absentInThisSystem in new[] { "Email", "Joined", "TwoFactor", "TwoFactorEnabled" })
        {
            Assert.IsFalse(
                markup.Contains(absentInThisSystem, StringComparison.Ordinal),
                $"'{absentInThisSystem}' is not a fact this system holds, so it is not a column here.");
        }
    }

    [TestMethod]
    public void ARow_OpensItsPerson_WithNoWayToChangeOneFromTheRoster()
    {
        var markup = PageMarkup();
        var codeBehind = CodeBehind();

        StringAssert.Contains(codeBehind, "ViewModel.OpenPersonCommand", "A row's whole job is to open that person (FR-087).");
        StringAssert.Contains(markup, "ItemClick=\"OnPersonInvoked\"", "Reaching any part of a row is what opens it.");
        StringAssert.Contains(markup, "Click=\"OnPersonLinkClick\"", "The name carries the row's keyboard way in.");

        foreach (var absentHere in new[] { "DeleteCommand", "RemoveCommand", "DeleteUser", "Export", "SelectionMode=\"Extended\"" })
        {
            Assert.IsFalse(
                markup.Contains(absentHere, StringComparison.Ordinal)
                    || codeBehind.Contains(absentHere, StringComparison.Ordinal),
                $"'{absentHere}' is not on this page: reset, deactivate and reactivate live on the person's own page, and deleting would erase the change history (settled decision 22).");
        }
    }

    [TestMethod]
    public void TheColumnWidths_AreSharedAndStopAtAMinimum_WithNoColumnPinnedToPixels()
    {
        // The table's own region, so the page's other grids are not measured as if they were columns of it.
        var table = Regex
            .Match(PageMarkup(), "<ListView\\.Header>(?<region>.*)</ListView\\.ItemTemplate>", RegexOptions.Singleline)
            .Groups["region"].Value;

        var definitions = Regex
            .Matches(table, "<ColumnDefinition(?<attributes>[^>]*)/>", RegexOptions.CultureInvariant)
            .Select(match => match.Groups["attributes"].Value.Trim())
            .ToList();

        // The heading row and the row template each carry the five columns, and the two sets have to be the same
        // set: a heading that is narrower than its cells stops naming the fact underneath it.
        Assert.AreEqual(10, definitions.Count, "Five columns, declared once for the headings and once for the rows.");

        var headings = definitions.Take(5).ToList();
        var rows = definitions.Skip(5).ToList();
        CollectionAssert.AreEqual(headings, rows, "The headings and the cells have to measure their columns the same way.");

        foreach (var definition in headings)
        {
            StringAssert.Contains(
                definition,
                "MinWidth=\"",
                "A column stops at a minimum and the table scrolls sideways past it, rather than the fact being dropped (decision 28).");
            Assert.IsFalse(
                Regex.IsMatch(definition, "(?<!Min)Width=\"[0-9]+\"", RegexOptions.CultureInvariant),
                $"A column carries no fixed width: a share of the window or Auto, never a number of pixels. This one reads '{definition}'.");
        }
    }

    [TestMethod]
    public void EveryStringThePageShows_IsAShippedResource()
    {
        var resources = File.ReadAllText(RepositoryPath("Strings/en-us/Resources.resw"));
        var sources = new[]
        {
            PagePath,
            CodeBehindPath,
            "MTM_Waitlist.Settings/ViewModels/UserManagementViewModel.cs",
            "MTM_Waitlist.Settings/Models/UserSummary.cs",
        };

        var keys = sources
            .SelectMany(path => Regex
                .Matches(File.ReadAllText(RepositoryPath(path)), "\"(?<key>UserManagement_[A-Za-z.]+)\"", RegexOptions.CultureInvariant)
                .Select(match => match.Groups["key"].Value))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.IsTrue(keys.Count > 20, $"Only {keys.Count} resource keys were found, so a clean result would prove nothing.");

        foreach (var key in keys)
        {
            StringAssert.Contains(
                resources,
                $"name=\"{key}\"",
                $"'{key}' is asked for by this screen and has no shipped entry, which would show the reader the key itself.");
        }
    }

    [TestMethod]
    public void TheFooterRange_IsShippedWithTheThreeValuesItPutsInIt()
    {
        var resources = File.ReadAllText(RepositoryPath("Strings/en-us/Resources.resw"));

        var range = Regex
            .Match(resources, "<data name=\"UserManagement_Pager.Range\"[^>]*>\\s*<value>(?<value>[^<]*)</value>", RegexOptions.CultureInvariant)
            .Groups["value"].Value;

        Assert.IsFalse(string.IsNullOrWhiteSpace(range), "The footer's range line has to be shipped, not spelled in code.");

        // Three values are handed to this string, so three holes are what it has to have: two characters fewer is a
        // formatting failure at the reader's screen rather than at the build.
        foreach (var placeholder in new[] { "{0}", "{1}", "{2}" })
        {
            StringAssert.Contains(range, placeholder, $"The range line as shipped is '{range}'.");
        }
    }

    private static string PageMarkup() => File.ReadAllText(RepositoryPath(PagePath));

    private static string CodeBehind() => File.ReadAllText(RepositoryPath(CodeBehindPath));

    private static string RepositoryPath(string relativePath) =>
        Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
}
