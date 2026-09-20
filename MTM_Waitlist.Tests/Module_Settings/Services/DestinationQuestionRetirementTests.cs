using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

/// <summary>
/// FR-054 and the 2026-09-20 decision of record (D22): a die always goes to its home location, so nothing about
/// <b>where</b> a die goes is asked, stored or rendered — and the value column the question used to occupy now
/// carries <b>which</b> die the request is for.
/// </summary>
/// <remarks>
/// <para>
/// This is the gate that fails if the retired question comes back. It is deliberately built from the two
/// shapes that actually carried it — the Line 2 context's <c>destination</c> value and a configuration row's
/// required <c>Destination</c> answer field — rather than from a bare word search, so honest retirement prose
/// (this file, <c>specs/</c>, the CSV's own change notes) stays legal while a live re-introduction does not.
/// </para>
/// <para>
/// <b>Two exemptions, both recorded rather than assumed.</b> <c>WeekendProject/</c> holds the workstream notes
/// that record the retirement, and the CSV's die rows carry their own change note; neither is live code, and
/// the notes cannot describe a removal without naming what was removed.
/// </para>
/// </remarks>
[TestClass]
public sealed class DestinationQuestionRetirementTests
{
    /// <summary>The two Items that raise a die request (FR-054).</summary>
    private static readonly string[] s_dieItems = ["pickup-die", "deliver-die"];

    // ── The retired value is gone from the code that carried it ─────────────────────────────────────────

    [TestMethod]
    public void TheLine2Context_CarriesNoDestinationValue()
    {
        // The context is the one place the captured destination was handed to the card resolver, so its absence
        // here is what proves no template can be shaped by where a die was going (FR-054, contract §3).
        Assert.IsNull(
            typeof(RequestItemLine2Context).GetProperty("Destination"),
            "RequestItemLine2Context still carries a Destination member, so a template can still be shaped by it.");

        Assert.IsNull(
            typeof(RequestItemLine2Resolver).GetField("HomeLocation"),
            "The resolver still pins the 'Home Location' answer the retired Take To question offered.");
    }

    [TestMethod]
    public void NoProductionSource_RegistersOrPassesTheRetiredDestinationToken()
    {
        // Identifier-shaped on purpose: the *token* and the *named argument* are what carried the question. The
        // word on its own is not evidence — the startup log's centralized destination and the flatstock row's
        // fixed Destination label are different things that must stay legal.
        (string Description, string Pattern)[] patterns =
        [
            ("the retired Line 2 token", @"\[\s*""destination""\s*\]"),
            ("the retired context value", @"\bDestination\s*:\s*answer\b"),
            ("the retired context member", @"\bcontext\.Destination\b"),
        ];

        var hits = RepositoryPatternScan.Scan(
            patterns,
            new RepositoryScanScope { ExcludedDirectories = [.. RepositoryPatternScan.DefaultExcludedDirectories, "WeekendProject"] }
                .ExcludingFiles("DestinationQuestionRetirementTests.cs"));

        Assert.AreEqual(
            0,
            hits.Count,
            "The retired destination question is still named in production source:"
                + Environment.NewLine
                + RepositoryPatternScan.Describe(hits));
    }

    [TestMethod]
    public void NoCatalogRow_NamesTheRetiredDestinationToken()
    {
        var offenders = RequestItemCatalog.Items
            .SelectMany(item => new[] { item.CardLine1Template, item.CardLine2Template }
                .Where(template => template?.Contains("{destination}", StringComparison.OrdinalIgnoreCase) == true)
                .Select(_ => item.Id))
            .ToArray();

        Assert.AreEqual(
            0,
            offenders.Length,
            $"These catalog rows still name the retired token: {string.Join(", ", offenders)}.");
    }

    [TestMethod]
    public void NoConfigurationRow_AsksWhereTheDieIsGoing()
    {
        foreach (var relativePath in SeedFiles())
        {
            var seed = File.ReadAllText(relativePath);

            foreach (var itemCode in s_dieItems)
            {
                var row = SeedRow(seed, itemCode);

                Assert.IsFalse(
                    row.Contains("'label','Destination','value_type','enum','source','answer'", StringComparison.Ordinal),
                    $"{Path.GetFileName(Path.GetDirectoryName(relativePath))}/{Path.GetFileName(relativePath)} still asks "
                        + $"{itemCode}'s requester where the die is going (FR-054).");
            }
        }
    }

    // ── And both die rows now ask which die ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void BothDieRows_AskWhichDieFromTheJobsOwnList_AndCarryNoListOfTheirOwn()
    {
        foreach (var relativePath in SeedFiles())
        {
            var seed = File.ReadAllText(relativePath);

            foreach (var itemCode in s_dieItems)
            {
                var row = SeedRow(seed, itemCode);
                var where = $"{Path.GetFileName(relativePath)} → {itemCode}";

                Assert.IsTrue(
                    row.Contains("'collect-input-then-confirm', 1, 'enum'", StringComparison.Ordinal),
                    $"{where} must ask for an answer of a choice kind, so the flow reaches the die step (FR-054).");
                Assert.IsTrue(
                    row.Contains("'value_type','enum','source','answer','list','die'", StringComparison.Ordinal),
                    $"{where} must name the job's die list on its answer field (FR-054).");
                Assert.IsTrue(
                    row.Contains("'list','die','order',1,'is_required',TRUE", StringComparison.Ordinal),
                    $"{where} must require the die, so a request can never be raised for no die at all.");
                Assert.IsFalse(
                    row.Contains("JSON_ARRAY('", StringComparison.Ordinal),
                    $"{where} ships with no list of its own: the choices are the job's dies (FR-054).");
            }
        }
    }

    [TestMethod]
    public void BothDieRows_ListTheLocationTheJobRecords_NotTheRetiredQuestion()
    {
        // FR-057: the page states where the die lives, and it is a job value — so it is the *job* row that
        // carries it, and no row asks the requester for it.
        foreach (var relativePath in SeedFiles())
        {
            var seed = File.ReadAllText(relativePath);

            foreach (var itemCode in s_dieItems)
            {
                var row = SeedRow(seed, itemCode);

                Assert.IsTrue(
                    row.Contains("'label','Pickup location','value_type','string','source','job'", StringComparison.Ordinal),
                    $"{itemCode} must show the location the requesting job records (FR-057).");
            }
        }
    }

    /// <summary>The shipped configuration seed, and the aggregate that mirrors it.</summary>
    private static IEnumerable<string> SeedFiles()
    {
        var root = RepositoryPatternScan.FindRepositoryRoot();

        return
        [
            Path.Combine(root, "Database", "Seeds", "seed_waitlist_request_item_configs", "create.sql"),
            Path.Combine(root, "Database", "Seeds", "AllSeeds.sql"),
        ];
    }

    /// <summary>
    /// The shipped INSERT row for one Item, from its Item code to the row's own terminator. Line endings are
    /// normalised first, because the two files are written with different ones and the row boundary is the
    /// blank line between two <c>VALUES</c> entries, not a comment.
    /// </summary>
    private static string SeedRow(string seedText, string itemCode)
    {
        var normalized = seedText.Replace("\r\n", "\n", StringComparison.Ordinal);
        var start = normalized.IndexOf("'" + itemCode + "'", StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"The shipped configuration seed no longer carries a row for {itemCode}.");

        var end = normalized.IndexOf("),\n\n", start, StringComparison.Ordinal);
        return end < 0 ? normalized[start..] : normalized[start..end];
    }
}
